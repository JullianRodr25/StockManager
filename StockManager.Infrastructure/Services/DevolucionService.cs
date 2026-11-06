using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

public class DevolucionService : IDevolucionService
{
    private readonly AppDbContext _dbContext;
    private readonly IStockNotificador _stockNotificador;
    private readonly ITurnoCajaService _turnoCajaService;

    public DevolucionService(
        AppDbContext dbContext,
        IStockNotificador stockNotificador,
        ITurnoCajaService turnoCajaService)
    {
        _dbContext = dbContext;
        _stockNotificador = stockNotificador;
        _turnoCajaService = turnoCajaService;
    }

    public async Task<DevolucionResponse> RegistrarAsync(int ventaId, RegistrarDevolucionRequest request, int empleadoId)
    {
        if (request.Devueltas is null || request.Devueltas.Count == 0)
            throw new ArgumentException("Selecciona al menos un producto para devolver.");

        if (request.Devueltas.Any(l => l.Cantidad <= 0) || request.Cambio?.Any(l => l.Cantidad <= 0) == true)
            throw new ArgumentException("Todas las cantidades deben ser mayores a 0.");

        if (request.Devueltas.GroupBy(l => l.DetalleVentaId).Any(g => g.Count() > 1))
            throw new ArgumentException("Una misma línea de la venta no puede aparecer dos veces en la devolución.");

        var venta = await _dbContext.Ventas.AsNoTracking().FirstOrDefaultAsync(v => v.Id == ventaId)
            ?? throw new VentaNoEncontradaException(ventaId);

        if (venta.EsCotizacion)
            throw new ArgumentException("Una cotización no se puede devolver: no es una venta.");

        // Una cuenta abierta todavía se puede corregir quitando líneas; solo lo ya pagado se devuelve.
        if (venta.Estado != "Pagada")
            throw new VentaEstadoInvalidoException(venta.Id, venta.Estado, "Pagada");

        var detallesVenta = await _dbContext.DetallesVenta.AsNoTracking()
            .Where(d => d.VentaId == ventaId)
            .ToListAsync();

        var yaDevuelto = await YaDevueltoPorLineaAsync(ventaId);

        // El cambio puede traer el mismo producto en varias filas: se unifica por producto.
        var cambio = (request.Cambio ?? [])
            .GroupBy(l => l.ProductoId)
            .Select(g => new LineaCambioRequest(g.Key, g.Sum(l => l.Cantidad)))
            .ToList();

        // Productos que se tocan: los devueltos (por su línea de venta) y los del cambio.
        var idsProducto = detallesVenta
            .Where(d => request.Devueltas.Any(l => l.DetalleVentaId == d.Id))
            .Select(d => d.ProductoId)
            .Concat(cambio.Select(c => c.ProductoId))
            .Distinct()
            .ToList();

        var productos = await _dbContext.Productos
            .Where(p => idsProducto.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        // 1) Validar lo devuelto contra la venta original y valorarlo a SU precio, no al de hoy.
        var devueltasValidadas = new List<(DetalleVenta Detalle, LineaDevolucionRequest Linea)>();
        foreach (var linea in request.Devueltas)
        {
            var detalle = detallesVenta.FirstOrDefault(d => d.Id == linea.DetalleVentaId)
                ?? throw new ArgumentException($"La línea {linea.DetalleVentaId} no pertenece a esta venta.");

            var disponible = detalle.Cantidad - yaDevuelto.GetValueOrDefault(detalle.Id);
            if (linea.Cantidad > disponible)
            {
                var nombre = productos.TryGetValue(detalle.ProductoId, out var p) ? p.Nombre : $"producto {detalle.ProductoId}";
                throw new ArgumentException(
                    disponible <= 0
                        ? $"'{nombre}' ya fue devuelto por completo."
                        : $"Solo se pueden devolver {disponible} unidad(es) de '{nombre}'.");
            }

            devueltasValidadas.Add((detalle, linea));
        }

        foreach (var c in cambio)
        {
            if (!productos.ContainsKey(c.ProductoId))
                throw new ArgumentException($"El producto con ID {c.ProductoId} no existe.");
        }

        var totalDevuelto = devueltasValidadas.Sum(x => x.Linea.Cantidad * x.Detalle.PrecioUnitario);
        var totalCambio = cambio.Sum(c => c.Cantidad * productos[c.ProductoId].Precio);
        var diferencia = totalCambio - totalDevuelto;

        // 2) La plata: un movimiento en efectivo exige caja abierta. Se resuelve ANTES de abrir la
        //    transacción para que, si no hay caja, no quede nada a medias.
        int? turnoCajaId = null;
        if (diferencia != 0)
        {
            turnoCajaId = await _turnoCajaService.ResolverTurnoParaPagoAsync(
                empleadoId, request.MetodoPago == "Efectivo");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var devolucion = Devolucion.Crear(
            ventaId, empleadoId, request.Motivo, totalDevuelto, totalCambio,
            request.MetodoPago, venta.RequiereFacturaElectronica);

        if (turnoCajaId.HasValue)
            devolucion.AsignarTurnoCaja(turnoCajaId.Value);

        _dbContext.Devoluciones.Add(devolucion);
        await _dbContext.SaveChangesAsync(); // Genera el Id que necesitan las líneas y los movimientos.

        var tocados = new Dictionary<int, Producto>();

        // 3) Primero lo que vuelve: así un cambio por el mismo producto no choca con un stock en cero.
        foreach (var (detalle, linea) in devueltasValidadas)
        {
            var producto = productos[detalle.ProductoId];

            _dbContext.DetallesDevolucion.Add(DetalleDevolucion.CrearDevuelto(
                devolucion.Id, detalle.Id, detalle.ProductoId, linea.Cantidad, detalle.PrecioUnitario, linea.ReintegrarAlStock));

            if (linea.ReintegrarAlStock)
            {
                producto.Reponer(linea.Cantidad);
                _dbContext.MovimientosStock.Add(MovimientoStock.Crear(
                    producto.Id, "EntradaDevolucion", linea.Cantidad, "Devolucion", devolucion.Id));
                tocados[producto.Id] = producto;
            }
        }

        // 4) Después lo que sale a cambio, al precio vigente.
        foreach (var c in cambio)
        {
            var producto = productos[c.ProductoId];
            producto.Vender(c.Cantidad); // Valida producto activo y stock suficiente.

            _dbContext.DetallesDevolucion.Add(DetalleDevolucion.CrearEntregado(
                devolucion.Id, producto.Id, c.Cantidad, producto.Precio));
            _dbContext.MovimientosStock.Add(MovimientoStock.Crear(
                producto.Id, "SalidaVenta", c.Cantidad, "Devolucion", devolucion.Id));
            tocados[producto.Id] = producto;
        }

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException(
                "El stock de uno de los productos cambió mientras se procesaba la devolución. Intenta de nuevo.");
        }

        await transaction.CommitAsync();
        await NotificarCambioStockAsync(tocados.Values);

        return (await ObtenerPorVentaAsync(ventaId)).First(d => d.Id == devolucion.Id);
    }

    public async Task<List<DevolucionResponse>> ObtenerPorVentaAsync(int ventaId)
    {
        var devoluciones = await _dbContext.Devoluciones.AsNoTracking()
            .Where(d => d.VentaId == ventaId)
            .OrderByDescending(d => d.Fecha)
            .ToListAsync();

        if (devoluciones.Count == 0)
            return [];

        var ids = devoluciones.Select(d => d.Id).ToList();

        var detalles = await (
            from dd in _dbContext.DetallesDevolucion.AsNoTracking()
            join p in _dbContext.Productos.AsNoTracking() on dd.ProductoId equals p.Id
            where ids.Contains(dd.DevolucionId)
            orderby dd.Id
            select new { dd, NombreProducto = p.Nombre })
            .ToListAsync();

        var idsEmpleado = devoluciones.Select(d => d.EmpleadoId).Distinct().ToList();
        var empleados = await _dbContext.Empleados.AsNoTracking()
            .Where(e => idsEmpleado.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Nombre);

        return devoluciones.Select(d => new DevolucionResponse(
            d.Id,
            d.VentaId,
            d.EmpleadoId,
            empleados.GetValueOrDefault(d.EmpleadoId),
            d.Fecha,
            d.Motivo,
            d.TotalDevuelto,
            d.TotalCambio,
            d.Diferencia,
            d.MetodoPago,
            d.RequiereNotaCredito,
            detalles
                .Where(x => x.dd.DevolucionId == d.Id)
                .Select(x => new DetalleDevolucionResponse(
                    x.dd.Id, x.dd.Sentido, x.dd.DetalleVentaId, x.dd.ProductoId, x.NombreProducto,
                    x.dd.Cantidad, x.dd.PrecioUnitario, x.dd.Subtotal, x.dd.ReintegradoAlStock))
                .ToList()
        )).ToList();
    }

    /// <summary>Unidades ya devueltas de cada línea de la venta (suma de todas las devoluciones anteriores).</summary>
    private async Task<Dictionary<int, int>> YaDevueltoPorLineaAsync(int ventaId)
    {
        var filas = await (
            from dd in _dbContext.DetallesDevolucion.AsNoTracking()
            join d in _dbContext.Devoluciones.AsNoTracking() on dd.DevolucionId equals d.Id
            where d.VentaId == ventaId && dd.Sentido == DetalleDevolucion.SentidoDevuelto
            select new { dd.DetalleVentaId, dd.Cantidad })
            .ToListAsync();

        return filas
            .Where(f => f.DetalleVentaId.HasValue)
            .GroupBy(f => f.DetalleVentaId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(f => f.Cantidad));
    }

    /// <summary>Best-effort, igual que en VentaService: un fallo del tiempo real no debe afectar la devolución ya guardada.</summary>
    private async Task NotificarCambioStockAsync(IEnumerable<Producto> productos)
    {
        try
        {
            var cambios = productos.Select(p => new CambioStockDto(p.Id, p.StockActual)).ToList();
            if (cambios.Count > 0)
                await _stockNotificador.NotificarCambiosAsync(cambios);
        }
        catch
        {
            // Ignorado a propósito.
        }
    }
}
