using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Domain.Events;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

public class VentaService : IVentaService
{
    private readonly AppDbContext _dbContext;
    private readonly IEventoNotificacionPublisher _eventoPublisher;
    private readonly IStockNotificador _stockNotificador;

    public VentaService(
        AppDbContext dbContext,
        IEventoNotificacionPublisher eventoPublisher,
        IStockNotificador stockNotificador)
    {
        _dbContext = dbContext;
        _eventoPublisher = eventoPublisher;
        _stockNotificador = stockNotificador;
    }

    /// <summary>
    /// Avisa a las pantallas conectadas que el stock de estos productos cambió, después de
    /// que el cambio ya quedó guardado. Nunca debe poder tumbar la venta/pedido que la
    /// disparó: si el Hub falla (ej. sin clientes conectados todavía), se ignora en
    /// silencio — el usuario que hizo la operación ya recibió su respuesta correcta.
    /// </summary>
    private async Task NotificarCambioStockAsync(IEnumerable<Producto> productos)
    {
        try
        {
            var cambios = productos
                .Select(p => new CambioStockDto(p.Id, p.StockActual))
                .ToList();
            await _stockNotificador.NotificarCambiosAsync(cambios);
        }
        catch
        {
            // Best-effort: un fallo al avisar en tiempo real no debe afectar la operación de negocio.
        }
    }

    public async Task<VentaResponse> RegistrarVentaAsync(RegistrarVentaRequest request, int empleadoId)
    {
        if (request.Lineas == null || request.Lineas.Count == 0)
            throw new ArgumentException("Debe incluir al menos un producto");

        Cliente? cliente = null;
        if (request.ClienteId.HasValue)
        {
            cliente = await _dbContext.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.ClienteId.Value);

            if (cliente is null)
                throw new ArgumentException($"El cliente con ID {request.ClienteId.Value} no existe.");
        }

        // Si se pidió factura electrónica, los datos fiscales de ESTA venta mandan sobre los
        // del perfil del Cliente (ej. quiere facturar a nombre de una empresa distinta); si la
        // venta no trajo alguno, se completa con lo guardado en el Cliente. Para un comprador
        // sin registrar, solo quedan los que haya traído la venta — Venta.Crear exige los
        // obligatorios y rechaza la venta si faltan.
        var tipoDocumentoFiscal = request.TipoDocumentoFiscal ?? cliente?.TipoDocumentoFiscal;
        var numeroDocumentoFiscal = request.NumeroDocumentoFiscal ?? cliente?.NumeroDocumentoFiscal;
        var razonSocialFiscal = request.RazonSocialFiscal ?? cliente?.RazonSocialFiscal;
        var direccionFiscal = request.DireccionFiscal ?? cliente?.DireccionFiscal;
        var emailFacturacionFiscal = request.EmailFacturacion ?? cliente?.EmailFacturacion;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var calculos = new List<CalculoLinea>();

        // FASE DE CÁLCULO: no modifica el stock.
        foreach (var linea in request.Lineas)
        {
            var producto = await _dbContext.Productos
                .FirstOrDefaultAsync(p => p.Id == linea.ProductoId);

            if (producto == null)
                throw new ArgumentException($"El producto con ID {linea.ProductoId} no existe.");

            if (!producto.Activo)
                throw new ProductoInactivoException(producto.Id, producto.Nombre);

            if (linea.Cantidad <= 0)
                throw new ArgumentException(
                    $"La cantidad para el producto '{producto.Nombre}' debe ser mayor a 0.");
            
            // Precio ya es el valor final que paga el cliente (IVA incluido cuando el producto
            // aplica IVA) — nunca se le suma IVA encima. Para la factura se descompone ese
            // bruto en base + IVA: iva = bruto * tarifa%, base = bruto - iva. Si el producto no
            // aplica IVA, TarifaIva siempre vale 0 (invariante del dominio), así que iva da 0 y
            // la base es igual al bruto.
            var bruto = producto.Precio * linea.Cantidad;
            var iva = bruto * (producto.TarifaIva / 100m);
            var subtotalSinIva = bruto - iva;
            var subtotalConIva = bruto;

            calculos.Add(new CalculoLinea(
                producto,
                linea.Cantidad,
                producto.Precio,
                subtotalSinIva,
                iva,
                subtotalConIva));
        }

        var total = calculos.Sum(c => c.SubtotalConIva);

        var venta = Venta.Crear(
            empleadoId,
            request.ClienteId,
            request.NombreComprador,
            request.TelefonoComprador,
            request.EmailComprador,
            request.MetodoPago,
            total,
            esCotizacion: false,
            estado: "Pagada",
            montoRecibido: request.MontoRecibido,
            detallesPago: AConTuplasDetallesPago(request.DetallesPago),
            requiereFacturaElectronica: request.RequiereFacturaElectronica,
            tipoDocumentoFacturado: tipoDocumentoFiscal,
            numeroDocumentoFacturado: numeroDocumentoFiscal,
            razonSocialFacturada: razonSocialFiscal,
            direccionFacturada: direccionFiscal,
            emailFacturacion: emailFacturacionFiscal);

        _dbContext.Ventas.Add(venta);
        await _dbContext.SaveChangesAsync();

        // FASE DE APLICACIÓN: modifica stock y crea los registros relacionados.
        foreach (var calculo in calculos)
        {
            calculo.Producto.Vender(calculo.Cantidad);

            var detalle = DetalleVenta.Crear(
                venta.Id,
                calculo.Producto.Id,
                calculo.Cantidad,
                calculo.PrecioUnitario);
            _dbContext.DetallesVenta.Add(detalle);

            var movimiento = MovimientoStock.Crear(
                calculo.Producto.Id,
                "SalidaVenta",
                calculo.Cantidad,
                "Venta",
                venta.Id);
            _dbContext.MovimientosStock.Add(movimiento);
        }

        // El desglose ya fue validado por Venta.Crear (suma == total); acá solo se persiste.
        if (venta.MetodoPago == "Mixto")
        {
            foreach (var linea in request.DetallesPago!)
                _dbContext.DetallesPagoVenta.Add(DetallePagoVenta.Crear(venta.Id, linea.MetodoPago, linea.Monto));
        }

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException(
                "El stock de uno de los productos cambió mientras se " +
                "procesaba la venta. Intenta de nuevo.");
        }

        await NotificarCambioStockAsync(calculos.Select(c => c.Producto));

        var factura = await GenerarFacturaAsync(venta.Id, venta.Total);

        await transaction.CommitAsync();

        var detalles = await ObtenerDetallesVentaAsync(venta.Id);
        var detallesPago = await ObtenerDetallesPagoAsync(venta.Id, venta.MetodoPago);

        return new VentaResponse(
            venta.Id,
            venta.ClienteId,
            venta.NombreComprador,
            venta.TelefonoComprador,
            venta.EmailComprador,
            venta.MetodoPago,
            venta.EmpleadoId,
            venta.Fecha,
            venta.Estado,
            venta.Total,
            factura.Numero!,
            detalles,
            venta.MontoRecibido,
            venta.Cambio,
            detallesPago,
            venta.RequiereFacturaElectronica,
            venta.EstadoFacturaElectronica,
            venta.TipoDocumentoFacturado,
            venta.NumeroDocumentoFacturado,
            venta.RazonSocialFacturada,
            venta.DireccionFacturada,
            venta.EmailFacturacion);
    }

    public async Task<(List<VentaResumenResponse> Items, int Total)> ObtenerVentasPaginadoAsync(
        int pagina, int tamanoPagina, DateTime? desde, DateTime? hasta, string? estado)
    {
        var query = _dbContext.Ventas.AsNoTracking().AsQueryable();

        if (desde.HasValue)
            query = query.Where(v => v.Fecha >= desde.Value);

        if (hasta.HasValue)
            query = query.Where(v => v.Fecha <= hasta.Value);

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(v => v.Estado == estado);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(v => v.Fecha)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .GroupJoin(
                _dbContext.Facturas.AsNoTracking(),
                venta => venta.Id,
                factura => factura.VentaId,
                (venta, facturas) => new { venta, facturas })
            .SelectMany(
                x => x.facturas.DefaultIfEmpty(),
                (x, factura) => new VentaResumenResponse(
                    x.venta.Id,
                    x.venta.NombreComprador,
                    x.venta.ClienteId,
                    x.venta.Fecha,
                    x.venta.Estado,
                    x.venta.Total,
                    x.venta.MetodoPago,
                    factura != null ? (factura.Numero ?? string.Empty) : string.Empty))
            .ToListAsync();

        return (items, total);
    }

    public async Task<VentaResponse?> ObtenerVentaPorIdAsync(int id)
    {
        var venta = await _dbContext.Ventas.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
        if (venta == null)
            return null;

        var numeroFactura = await _dbContext.Facturas
            .AsNoTracking()
            .Where(f => f.VentaId == id)
            .Select(f => f.Numero)
            .FirstOrDefaultAsync() ?? string.Empty;

        var detalles = await ObtenerDetallesVentaAsync(id);
        var detallesPago = await ObtenerDetallesPagoAsync(id, venta.MetodoPago);

        return new VentaResponse(
            venta.Id,
            venta.ClienteId,
            venta.NombreComprador,
            venta.TelefonoComprador,
            venta.EmailComprador,
            venta.MetodoPago,
            venta.EmpleadoId,
            venta.Fecha,
            venta.Estado,
            venta.Total,
            numeroFactura,
            detalles,
            venta.MontoRecibido,
            venta.Cambio,
            detallesPago,
            venta.RequiereFacturaElectronica,
            venta.EstadoFacturaElectronica,
            venta.TipoDocumentoFacturado,
            venta.NumeroDocumentoFacturado,
            venta.RazonSocialFacturada,
            venta.DireccionFacturada,
            venta.EmailFacturacion);
    }

    public async Task<VentaResponse> AbrirFiadoAsync(int clienteId, int empleadoId)
    {
        var cliente = await _dbContext.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId);
        if (cliente is null)
            throw new ArgumentException($"El cliente con ID {clienteId} no existe.");

        var tieneCuentaAbierta = await _dbContext.Ventas
            .AnyAsync(v => v.ClienteId == clienteId && v.Estado == "Pendiente");
        if (tieneCuentaAbierta)
            throw new CuentaFiadoAbiertaException(clienteId);

        var venta = Venta.AbrirFiado(empleadoId, clienteId, cliente.Nombre);
        _dbContext.Ventas.Add(venta);
        await _dbContext.SaveChangesAsync();

        return new VentaResponse(
            venta.Id,
            venta.ClienteId,
            venta.NombreComprador,
            venta.TelefonoComprador,
            venta.EmailComprador,
            venta.MetodoPago,
            venta.EmpleadoId,
            venta.Fecha,
            venta.Estado,
            venta.Total,
            string.Empty,
            new List<DetalleVentaResponse>(),
            RequiereFacturaElectronica: venta.RequiereFacturaElectronica,
            EstadoFacturaElectronica: venta.EstadoFacturaElectronica,
            TipoDocumentoFacturado: venta.TipoDocumentoFacturado,
            NumeroDocumentoFacturado: venta.NumeroDocumentoFacturado,
            RazonSocialFacturada: venta.RazonSocialFacturada,
            DireccionFacturada: venta.DireccionFacturada,
            EmailFacturacion: venta.EmailFacturacion);
    }

    public async Task<VentaResponse> AgregarLineaFiadoAsync(int ventaId, LineaVentaRequest linea)
    {
        if (linea.Cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a 0.");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var venta = await _dbContext.Ventas.FirstOrDefaultAsync(v => v.Id == ventaId);
        if (venta == null)
            throw new VentaNoEncontradaException(ventaId);

        if (venta.Estado != "Pendiente")
            throw new VentaEstadoInvalidoException(venta.Id, venta.Estado, "Pendiente");

        var producto = await _dbContext.Productos.FirstOrDefaultAsync(p => p.Id == linea.ProductoId);
        if (producto == null)
            throw new ArgumentException($"El producto con ID {linea.ProductoId} no existe.");

        // Ver RegistrarVentaAsync: Precio ya incluye el IVA, se descompone, no se suma.
        var bruto = producto.Precio * linea.Cantidad;
        var subtotalConIva = bruto;

        producto.Vender(linea.Cantidad);

        var detalle = DetalleVenta.Crear(venta.Id, producto.Id, linea.Cantidad, producto.Precio);
        _dbContext.DetallesVenta.Add(detalle);

        var movimiento = MovimientoStock.Crear(producto.Id, "SalidaVenta", linea.Cantidad, "Venta", venta.Id);
        _dbContext.MovimientosStock.Add(movimiento);

        venta.AgregarMonto(subtotalConIva);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException(
                "El stock de uno de los productos cambió mientras se " +
                "procesaba la línea. Intenta de nuevo.");
        }

        await NotificarCambioStockAsync([producto]);

        await transaction.CommitAsync();

        var detalles = await ObtenerDetallesVentaAsync(venta.Id);

        return new VentaResponse(
            venta.Id,
            venta.ClienteId,
            venta.NombreComprador,
            venta.TelefonoComprador,
            venta.EmailComprador,
            venta.MetodoPago,
            venta.EmpleadoId,
            venta.Fecha,
            venta.Estado,
            venta.Total,
            string.Empty,
            detalles,
            RequiereFacturaElectronica: venta.RequiereFacturaElectronica,
            EstadoFacturaElectronica: venta.EstadoFacturaElectronica,
            TipoDocumentoFacturado: venta.TipoDocumentoFacturado,
            NumeroDocumentoFacturado: venta.NumeroDocumentoFacturado,
            RazonSocialFacturada: venta.RazonSocialFacturada,
            DireccionFacturada: venta.DireccionFacturada,
            EmailFacturacion: venta.EmailFacturacion);
    }

    public async Task<VentaResponse> CerrarFiadoAsync(int ventaId, string metodoPago, decimal? montoRecibido = null, List<DetallePagoRequest>? detallesPago = null)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var venta = await _dbContext.Ventas.FirstOrDefaultAsync(v => v.Id == ventaId);
        if (venta == null)
            throw new VentaNoEncontradaException(ventaId);

        if (venta.Total <= 0)
            throw new ArgumentException(
                "No se puede cerrar una cuenta fiada sin productos agregados.");

        venta.CerrarFiado(metodoPago, montoRecibido, AConTuplasDetallesPago(detallesPago));

        // El desglose ya fue validado por Venta.CerrarFiado (suma == total); acá solo se persiste.
        if (venta.MetodoPago == "Mixto")
        {
            foreach (var linea in detallesPago!)
                _dbContext.DetallesPagoVenta.Add(DetallePagoVenta.Crear(venta.Id, linea.MetodoPago, linea.Monto));
        }

        await _dbContext.SaveChangesAsync();

        var factura = await GenerarFacturaAsync(venta.Id, venta.Total);

        await transaction.CommitAsync();

        var detalles = await ObtenerDetallesVentaAsync(venta.Id);
        var detallesPagoRespuesta = await ObtenerDetallesPagoAsync(venta.Id, venta.MetodoPago);

        return new VentaResponse(
            venta.Id,
            venta.ClienteId,
            venta.NombreComprador,
            venta.TelefonoComprador,
            venta.EmailComprador,
            venta.MetodoPago,
            venta.EmpleadoId,
            venta.Fecha,
            venta.Estado,
            venta.Total,
            factura.Numero!,
            detalles,
            venta.MontoRecibido,
            venta.Cambio,
            detallesPagoRespuesta,
            venta.RequiereFacturaElectronica,
            venta.EstadoFacturaElectronica,
            venta.TipoDocumentoFacturado,
            venta.NumeroDocumentoFacturado,
            venta.RazonSocialFacturada,
            venta.DireccionFacturada,
            venta.EmailFacturacion);
    }

    public async Task<VentaResponse> RegistrarAbonoAsync(int ventaId, decimal monto, string metodoPago, int empleadoId)
    {
        if (monto <= 0)
            throw new ArgumentException("El monto debe ser mayor a 0.");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var venta = await _dbContext.Ventas.FirstOrDefaultAsync(v => v.Id == ventaId);
        if (venta == null)
            throw new VentaNoEncontradaException(ventaId);

        if (venta.Estado != "Pendiente")
            throw new VentaEstadoInvalidoException(venta.Id, venta.Estado, "Pendiente");

        var totalAbonado = await _dbContext.AbonosCuenta
            .Where(a => a.VentaId == ventaId)
            .SumAsync(a => (decimal?)a.Monto) ?? 0m;

        var saldoPendiente = venta.Total - totalAbonado;

        if (monto > saldoPendiente)
            throw new ArgumentException(
                $"El monto ({monto:F2}) no puede ser mayor al saldo pendiente ({saldoPendiente:F2}).");

        var abono = AbonoCuenta.Crear(ventaId, monto, metodoPago, empleadoId);
        _dbContext.AbonosCuenta.Add(abono);
        await _dbContext.SaveChangesAsync();

        var numeroFactura = string.Empty;
        var nuevoSaldo = saldoPendiente - monto;

        if (nuevoSaldo == 0)
        {
            var metodosPagoUsados = await _dbContext.AbonosCuenta
                .Where(a => a.VentaId == ventaId)
                .Select(a => a.MetodoPago)
                .Distinct()
                .ToListAsync();

            var metodoPagoFinal = metodosPagoUsados.Count == 1 ? metodosPagoUsados[0] : "Mixto";

            // Si la cuenta se cierra en "Mixto" (los abonos usaron más de un método), el
            // desglose real para validar Venta.CerrarFiado se arma agrupando los propios
            // abonos ya guardados — no se duplica en DetallesPagoVenta porque ObtenerDetallesPagoAsync
            // ya sabe derivarlo de ahí para la respuesta (ver ese método).
            List<(string MetodoPago, decimal Monto)>? detallesParaValidar = null;
            if (metodoPagoFinal == "Mixto")
            {
                var montosPorMetodo = await _dbContext.AbonosCuenta
                    .Where(a => a.VentaId == ventaId)
                    .GroupBy(a => a.MetodoPago)
                    .Select(g => new { Metodo = g.Key, Monto = g.Sum(a => a.Monto) })
                    .ToListAsync();
                detallesParaValidar = montosPorMetodo.Select(x => (x.Metodo, x.Monto)).ToList();
            }

            // Un abono nunca puede exceder el saldo pendiente (se valida más arriba), así que
            // si el último abono que cierra la cuenta fue en efectivo, lo recibido siempre
            // coincide exactamente con el total — no hay vuelto que dar en este camino.
            var montoRecibidoCierre = metodoPagoFinal == "Efectivo" ? venta.Total : (decimal?)null;
            venta.CerrarFiado(metodoPagoFinal, montoRecibidoCierre, detallesParaValidar);
            await _dbContext.SaveChangesAsync();

            var factura = await GenerarFacturaAsync(venta.Id, venta.Total);
            numeroFactura = factura.Numero!;
        }

        await transaction.CommitAsync();

        var detalles = await ObtenerDetallesVentaAsync(venta.Id);
        var detallesPago = await ObtenerDetallesPagoAsync(venta.Id, venta.MetodoPago);

        return new VentaResponse(
            venta.Id,
            venta.ClienteId,
            venta.NombreComprador,
            venta.TelefonoComprador,
            venta.EmailComprador,
            venta.MetodoPago,
            venta.EmpleadoId,
            venta.Fecha,
            venta.Estado,
            venta.Total,
            numeroFactura,
            detalles,
            venta.MontoRecibido,
            venta.Cambio,
            detallesPago,
            venta.RequiereFacturaElectronica,
            venta.EstadoFacturaElectronica,
            venta.TipoDocumentoFacturado,
            venta.NumeroDocumentoFacturado,
            venta.RazonSocialFacturada,
            venta.DireccionFacturada,
            venta.EmailFacturacion);
    }

    public async Task<List<AbonoResponse>> ObtenerAbonosAsync(int ventaId)
    {
        return await _dbContext.AbonosCuenta
            .AsNoTracking()
            .Where(a => a.VentaId == ventaId)
            .OrderBy(a => a.Fecha)
            .Select(a => new AbonoResponse(a.Id, a.VentaId, a.Monto, a.MetodoPago, a.Fecha, a.EmpleadoId))
            .ToListAsync();
    }

    public async Task<VentaResponse> EditarCantidadLineaAsync(int ventaId, int detalleId, int nuevaCantidad)
    {
        if (nuevaCantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a 0.");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var venta = await _dbContext.Ventas.FirstOrDefaultAsync(v => v.Id == ventaId);
        if (venta == null)
            throw new VentaNoEncontradaException(ventaId);

        if (venta.Estado != "Pendiente")
            throw new VentaEstadoInvalidoException(venta.Id, venta.Estado, "Pendiente");

        var detalle = await _dbContext.DetallesVenta
            .FirstOrDefaultAsync(d => d.Id == detalleId && d.VentaId == ventaId);
        if (detalle == null)
            throw new ArgumentException($"El detalle con ID {detalleId} no existe en la venta {ventaId}.");

        var producto = await _dbContext.Productos.FirstOrDefaultAsync(p => p.Id == detalle.ProductoId);
        if (producto == null)
            throw new ArgumentException($"El producto con ID {detalle.ProductoId} no existe.");

        var diferencia = nuevaCantidad - detalle.Cantidad;
        if (diferencia == 0)
            throw new ArgumentException("La nueva cantidad es igual a la actual.");

        // PrecioUnitario ya es el valor final (IVA incluido si aplica) que se cobró por unidad,
        // así que el impacto de cambiar la cantidad es una simple multiplicación directa — no
        // hay que volver a sumarle IVA encima (ver RegistrarVentaAsync).
        var deltaMonetario = diferencia * detalle.PrecioUnitario;

        if (deltaMonetario < 0)
        {
            var totalAbonado = await _dbContext.AbonosCuenta
                .Where(a => a.VentaId == ventaId)
                .SumAsync(a => (decimal?)a.Monto) ?? 0m;

            var nuevoTotal = venta.Total + deltaMonetario;
            if (nuevoTotal < totalAbonado)
                throw new OperacionInvalidaCuentaFiadaException(
                    $"No se puede reducir la cantidad: el nuevo total ({nuevoTotal:F2}) quedaría por " +
                    $"debajo de lo ya abonado ({totalAbonado:F2}).");
        }

        if (diferencia > 0)
            producto.Vender(diferencia);
        else
            producto.Reponer(-diferencia);

        var movimiento = MovimientoStock.Crear(producto.Id, "Ajuste", Math.Abs(diferencia), "Venta", venta.Id);
        _dbContext.MovimientosStock.Add(movimiento);

        if (deltaMonetario > 0)
            venta.AgregarMonto(deltaMonetario);
        else
            venta.RestarMonto(-deltaMonetario);

        detalle.ActualizarCantidad(nuevaCantidad);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException(
                "El stock de uno de los productos cambió mientras se " +
                "procesaba el ajuste. Intenta de nuevo.");
        }

        await NotificarCambioStockAsync([producto]);

        await transaction.CommitAsync();

        var detalles = await ObtenerDetallesVentaAsync(venta.Id);

        return new VentaResponse(
            venta.Id,
            venta.ClienteId,
            venta.NombreComprador,
            venta.TelefonoComprador,
            venta.EmailComprador,
            venta.MetodoPago,
            venta.EmpleadoId,
            venta.Fecha,
            venta.Estado,
            venta.Total,
            string.Empty,
            detalles,
            RequiereFacturaElectronica: venta.RequiereFacturaElectronica,
            EstadoFacturaElectronica: venta.EstadoFacturaElectronica,
            TipoDocumentoFacturado: venta.TipoDocumentoFacturado,
            NumeroDocumentoFacturado: venta.NumeroDocumentoFacturado,
            RazonSocialFacturada: venta.RazonSocialFacturada,
            DireccionFacturada: venta.DireccionFacturada,
            EmailFacturacion: venta.EmailFacturacion);
    }

    public async Task<VentaResponse> QuitarLineaAsync(int ventaId, int detalleId)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var venta = await _dbContext.Ventas.FirstOrDefaultAsync(v => v.Id == ventaId);
        if (venta == null)
            throw new VentaNoEncontradaException(ventaId);

        if (venta.Estado != "Pendiente")
            throw new VentaEstadoInvalidoException(venta.Id, venta.Estado, "Pendiente");

        var detalle = await _dbContext.DetallesVenta
            .FirstOrDefaultAsync(d => d.Id == detalleId && d.VentaId == ventaId);
        if (detalle == null)
            throw new ArgumentException($"El detalle con ID {detalleId} no existe en la venta {ventaId}.");

        var cantidadLineas = await _dbContext.DetallesVenta.CountAsync(d => d.VentaId == ventaId);
        if (cantidadLineas <= 1)
            throw new OperacionInvalidaCuentaFiadaException(
                "No se puede quitar el último producto de la cuenta. Si quieres deshacer la cuenta " +
                "por completo, usa la opción de cancelar la cuenta.");

        var producto = await _dbContext.Productos.FirstOrDefaultAsync(p => p.Id == detalle.ProductoId);
        if (producto == null)
            throw new ArgumentException($"El producto con ID {detalle.ProductoId} no existe.");

        // PrecioUnitario ya es el valor final cobrado (ver RegistrarVentaAsync); quitar la línea
        // resta exactamente eso del total, sin volver a aplicarle IVA.
        var subtotalConIva = detalle.PrecioUnitario * detalle.Cantidad;

        var totalAbonado = await _dbContext.AbonosCuenta
            .Where(a => a.VentaId == ventaId)
            .SumAsync(a => (decimal?)a.Monto) ?? 0m;

        var nuevoTotal = venta.Total - subtotalConIva;
        if (nuevoTotal < totalAbonado)
            throw new OperacionInvalidaCuentaFiadaException(
                $"No se puede quitar este producto: el nuevo total ({nuevoTotal:F2}) quedaría por " +
                $"debajo de lo ya abonado ({totalAbonado:F2}).");

        producto.Reponer(detalle.Cantidad);

        var movimiento = MovimientoStock.Crear(producto.Id, "Ajuste", detalle.Cantidad, "Venta", venta.Id);
        _dbContext.MovimientosStock.Add(movimiento);

        venta.RestarMonto(subtotalConIva);

        _dbContext.DetallesVenta.Remove(detalle);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException(
                "El stock de uno de los productos cambió mientras se " +
                "procesaba la eliminación de la línea. Intenta de nuevo.");
        }

        await NotificarCambioStockAsync([producto]);

        await transaction.CommitAsync();

        var detalles = await ObtenerDetallesVentaAsync(venta.Id);

        return new VentaResponse(
            venta.Id,
            venta.ClienteId,
            venta.NombreComprador,
            venta.TelefonoComprador,
            venta.EmailComprador,
            venta.MetodoPago,
            venta.EmpleadoId,
            venta.Fecha,
            venta.Estado,
            venta.Total,
            string.Empty,
            detalles,
            RequiereFacturaElectronica: venta.RequiereFacturaElectronica,
            EstadoFacturaElectronica: venta.EstadoFacturaElectronica,
            TipoDocumentoFacturado: venta.TipoDocumentoFacturado,
            NumeroDocumentoFacturado: venta.NumeroDocumentoFacturado,
            RazonSocialFacturada: venta.RazonSocialFacturada,
            DireccionFacturada: venta.DireccionFacturada,
            EmailFacturacion: venta.EmailFacturacion);
    }

    /// <summary>
    /// Cancela por completo una cuenta fiada: exige que esté "Pendiente" y que no tenga abonos
    /// registrados, repone el stock de todas sus líneas y pasa la venta a "Cancelada".
    /// </summary>
    public async Task<VentaResponse> CancelarCuentaAsync(int ventaId)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var venta = await _dbContext.Ventas.FirstOrDefaultAsync(v => v.Id == ventaId);
        if (venta == null)
            throw new VentaNoEncontradaException(ventaId);

        if (venta.Estado != "Pendiente")
            throw new VentaEstadoInvalidoException(venta.Id, venta.Estado, "Pendiente");

        var tieneAbonos = await _dbContext.AbonosCuenta.AnyAsync(a => a.VentaId == ventaId);
        if (tieneAbonos)
            throw new CuentaConAbonosException(ventaId);

        var detalles = await _dbContext.DetallesVenta.Where(d => d.VentaId == ventaId).ToListAsync();
        var productosAfectados = new List<Producto>();

        foreach (var detalle in detalles)
        {
            var producto = await _dbContext.Productos.FirstOrDefaultAsync(p => p.Id == detalle.ProductoId);
            if (producto == null)
                throw new ArgumentException($"El producto con ID {detalle.ProductoId} no existe.");

            producto.Reponer(detalle.Cantidad);
            productosAfectados.Add(producto);

            var movimiento = MovimientoStock.Crear(producto.Id, "Ajuste", detalle.Cantidad, "Venta", venta.Id);
            _dbContext.MovimientosStock.Add(movimiento);

            _dbContext.DetallesVenta.Remove(detalle);
        }

        venta.CancelarCuenta();

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException(
                "El stock de uno de los productos cambió mientras se " +
                "procesaba la cancelación de la cuenta. Intenta de nuevo.");
        }

        await NotificarCambioStockAsync(productosAfectados);

        await transaction.CommitAsync();

        return new VentaResponse(
            venta.Id,
            venta.ClienteId,
            venta.NombreComprador,
            venta.TelefonoComprador,
            venta.EmailComprador,
            venta.MetodoPago,
            venta.EmpleadoId,
            venta.Fecha,
            venta.Estado,
            venta.Total,
            string.Empty,
            new List<DetalleVentaResponse>(),
            RequiereFacturaElectronica: venta.RequiereFacturaElectronica,
            EstadoFacturaElectronica: venta.EstadoFacturaElectronica,
            TipoDocumentoFacturado: venta.TipoDocumentoFacturado,
            NumeroDocumentoFacturado: venta.NumeroDocumentoFacturado,
            RazonSocialFacturada: venta.RazonSocialFacturada,
            DireccionFacturada: venta.DireccionFacturada,
            EmailFacturacion: venta.EmailFacturacion);
    }

    /// <summary>
    /// Genera y persiste la Factura de una venta: se guarda primero sin Numero para obtener
    /// el Id real asignado por SQL Server, y recién con ese Id se genera el correlativo definitivo.
    /// </summary>
    private async Task<Factura> GenerarFacturaAsync(int ventaId, decimal total)
    {
        var factura = Factura.Crear(ventaId, null, total);
        _dbContext.Facturas.Add(factura);
        await _dbContext.SaveChangesAsync();

        factura.GenerarNumero();
        await _dbContext.SaveChangesAsync();

        // Único punto donde se sabe que el pago quedó completo (venta de mostrador
        // pagada al instante, o cuenta fiada recién saldada): dispara el envío de la
        // factura por WhatsApp (ver WhatsAppNotificationBackgroundService).
        _eventoPublisher.Publicar(new FacturaGeneradaEvent(factura.Id));

        return factura;
    }

    /// <summary>
    /// Convierte el desglose de pago que llega del DTO al formato de tuplas que entiende el
    /// dominio (Venta.Crear/Venta.CerrarFiado), para no filtrar un tipo de Application hacia
    /// Domain.
    /// </summary>
    private static List<(string MetodoPago, decimal Monto)>? AConTuplasDetallesPago(List<DetallePagoRequest>? detallesPago) =>
        detallesPago?.Select(d => (d.MetodoPago, d.Monto)).ToList();

    /// <summary>
    /// Devuelve el desglose de pago de una venta para mostrarlo en VentaResponse. Si el método
    /// no es "Mixto" no hay nada que desglosar (ni vale la pena consultar la base de datos).
    /// Para una venta/cierre directo con "Mixto", el desglose vive en DetallesPagoVenta; para
    /// una cuenta fiada que llegó a "Mixto" porque sus abonos usaron métodos distintos (ver
    /// RegistrarAbonoAsync), no se duplica el concepto de "pago" creando también filas en
    /// DetallesPagoVenta — en su lugar, se deriva agrupando los abonos ya guardados.
    /// </summary>
    private async Task<List<DetallePagoResponse>> ObtenerDetallesPagoAsync(int ventaId, string? metodoPago)
    {
        if (metodoPago != "Mixto")
            return new List<DetallePagoResponse>();

        var deVentaDirecta = await _dbContext.DetallesPagoVenta
            .AsNoTracking()
            .Where(d => d.VentaId == ventaId)
            .Select(d => new DetallePagoResponse(d.MetodoPago, d.Monto))
            .ToListAsync();

        if (deVentaDirecta.Count > 0)
            return deVentaDirecta;

        return await _dbContext.AbonosCuenta
            .AsNoTracking()
            .Where(a => a.VentaId == ventaId)
            .GroupBy(a => a.MetodoPago)
            .Select(g => new DetallePagoResponse(g.Key, g.Sum(a => a.Monto)))
            .ToListAsync();
    }

    private async Task<List<DetalleVentaResponse>> ObtenerDetallesVentaAsync(int ventaId)
    {
        return await _dbContext.DetallesVenta
            .AsNoTracking()
            .Where(d => d.VentaId == ventaId)
            .Join(
                _dbContext.Productos.AsNoTracking(),
                detalle => detalle.ProductoId,
                producto => producto.Id,
                // PrecioUnitario ya es el valor final cobrado por unidad (IVA incluido cuando el
                // producto aplica); acá se descompone en base + IVA para mostrarlo desglosado en
                // la factura, nunca se le suma IVA encima (ver RegistrarVentaAsync).
                (detalle, producto) => new DetalleVentaResponse(
                    detalle.Id,
                    detalle.ProductoId,
                    producto.Nombre,
                    detalle.Cantidad,
                    detalle.PrecioUnitario,
                    (detalle.PrecioUnitario * detalle.Cantidad) - (detalle.PrecioUnitario * detalle.Cantidad * (producto.TarifaIva / 100m)),
                    detalle.PrecioUnitario * detalle.Cantidad * (producto.TarifaIva / 100m),
                    detalle.PrecioUnitario * detalle.Cantidad))
            .ToListAsync();
    }

    private sealed record CalculoLinea(
        Producto Producto,
        int Cantidad,
        decimal PrecioUnitario,
        decimal SubtotalSinIva,
        decimal Iva,
        decimal SubtotalConIva);

    public async Task<List<ProductoVentaRecienteResponse>> ObtenerProductosRecientesAsync(int limite = 10)
    {
        // Se trae una ventana acotada de las líneas de venta más recientes (no canceladas) ya
        // ordenadas por fecha, y la deduplicación por producto (quedarse con la primera, que es
        // la más reciente) se hace en memoria: así se evita un GROUP BY que chocaría con "más
        // reciente", que no es una operación de agregación. La ventana (10x el límite pedido)
        // es generosa para cubrir el caso de un cajero que vende el mismo producto varias veces
        // seguidas sin que eso "tape" el cupo de productos distintos.
        var lineasRecientes = await _dbContext.DetallesVenta
            .AsNoTracking()
            .Join(
                _dbContext.Ventas.AsNoTracking().Where(v => v.Estado != "Cancelada"),
                detalle => detalle.VentaId,
                venta => venta.Id,
                (detalle, venta) => new { detalle.ProductoId, venta.Fecha })
            .OrderByDescending(x => x.Fecha)
            .Take(Math.Max(limite, 1) * 10)
            .ToListAsync();

        var productoIdsRecientes = lineasRecientes
            .GroupBy(x => x.ProductoId)
            .Select(g => g.Key)
            .Take(Math.Max(limite, 1))
            .ToList();

        // El orden de productoIdsRecientes ya es el de más a menos reciente, pero el Where de
        // abajo no lo conserva, así que se reordena en memoria uniendo contra un diccionario.
        var productosPorId = await _dbContext.Productos
            .AsNoTracking()
            .Where(p => productoIdsRecientes.Contains(p.Id) && p.Activo)
            .ToDictionaryAsync(p => p.Id);

        return productoIdsRecientes
            .Where(productosPorId.ContainsKey)
            .Select(id => new ProductoVentaRecienteResponse(id, productosPorId[id].Nombre))
            .ToList();
    }
}
