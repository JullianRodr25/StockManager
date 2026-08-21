using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

public class VentaService : IVentaService
{
    private readonly AppDbContext _dbContext;

    public VentaService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<VentaResponse> RegistrarVentaAsync(RegistrarVentaRequest request, int empleadoId)
    {
        if (request.Lineas == null || request.Lineas.Count == 0)
            throw new ArgumentException("Debe incluir al menos un producto");

        if (request.ClienteId.HasValue)
        {
            var clienteExiste = await _dbContext.Clientes
                .AnyAsync(c => c.Id == request.ClienteId.Value);

            if (!clienteExiste)
                throw new ArgumentException($"El cliente con ID {request.ClienteId.Value} no existe.");
        }

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
            
            var subtotalSinIva = producto.Precio * linea.Cantidad;
            var iva = subtotalSinIva * (producto.TarifaIva / 100m);
            var subtotalConIva = subtotalSinIva + iva;

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
            estado: "Pagada");

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

        var factura = await GenerarFacturaAsync(venta.Id, venta.Total);

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
            factura.Numero!,
            detalles);
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
            detalles);
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
            new List<DetalleVentaResponse>());
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

        var subtotalSinIva = producto.Precio * linea.Cantidad;
        var iva = subtotalSinIva * (producto.TarifaIva / 100m);
        var subtotalConIva = subtotalSinIva + iva;

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
            detalles);
    }

    public async Task<VentaResponse> CerrarFiadoAsync(int ventaId, string metodoPago)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var venta = await _dbContext.Ventas.FirstOrDefaultAsync(v => v.Id == ventaId);
        if (venta == null)
            throw new VentaNoEncontradaException(ventaId);

        if (venta.Total <= 0)
            throw new ArgumentException(
                "No se puede cerrar una cuenta fiada sin productos agregados.");

        venta.CerrarFiado(metodoPago);
        await _dbContext.SaveChangesAsync();

        var factura = await GenerarFacturaAsync(venta.Id, venta.Total);

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
            factura.Numero!,
            detalles);
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

            venta.CerrarFiado(metodoPagoFinal);
            await _dbContext.SaveChangesAsync();

            var factura = await GenerarFacturaAsync(venta.Id, venta.Total);
            numeroFactura = factura.Numero!;
        }

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
            numeroFactura,
            detalles);
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

        if (diferencia > 0)
            producto.Vender(diferencia);
        else
            producto.Reponer(-diferencia);

        var movimiento = MovimientoStock.Crear(producto.Id, "Ajuste", Math.Abs(diferencia), "Venta", venta.Id);
        _dbContext.MovimientosStock.Add(movimiento);

        var deltaMonetario = diferencia * detalle.PrecioUnitario * (1m + producto.TarifaIva / 100m);
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
            detalles);
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

        var producto = await _dbContext.Productos.FirstOrDefaultAsync(p => p.Id == detalle.ProductoId);
        if (producto == null)
            throw new ArgumentException($"El producto con ID {detalle.ProductoId} no existe.");

        producto.Reponer(detalle.Cantidad);

        var movimiento = MovimientoStock.Crear(producto.Id, "Ajuste", detalle.Cantidad, "Venta", venta.Id);
        _dbContext.MovimientosStock.Add(movimiento);

        var subtotalConIva = detalle.PrecioUnitario * detalle.Cantidad * (1m + producto.TarifaIva / 100m);
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
            detalles);
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

        return factura;
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
                (detalle, producto) => new DetalleVentaResponse(
                    detalle.Id,
                    detalle.ProductoId,
                    producto.Nombre,
                    detalle.Cantidad,
                    detalle.PrecioUnitario,
                    detalle.PrecioUnitario * detalle.Cantidad,
                    detalle.PrecioUnitario * detalle.Cantidad * (producto.TarifaIva / 100m),
                    detalle.PrecioUnitario * detalle.Cantidad * (1m + producto.TarifaIva / 100m)))
            .ToListAsync();
    }

    private sealed record CalculoLinea(
        Producto Producto,
        int Cantidad,
        decimal PrecioUnitario,
        decimal SubtotalSinIva,
        decimal Iva,
        decimal SubtotalConIva);
}
