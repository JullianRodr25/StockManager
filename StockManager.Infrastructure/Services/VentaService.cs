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

        // Generación de Factura: se guarda primero sin Numero para obtener el Id real
        // asignado por SQL Server, y recién con ese Id se genera el correlativo definitivo.
        var factura = Factura.Crear(venta.Id, null, venta.Total);
        _dbContext.Facturas.Add(factura);
        await _dbContext.SaveChangesAsync();

        factura.GenerarNumero();
        await _dbContext.SaveChangesAsync();

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
