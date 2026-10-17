using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Common;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

/// <summary>
/// Reportes administrativos de solo lectura. Reglas comunes:
///  - "Venta" = Estado "Pagada" y no cotización (las cuentas abiertas "Pendiente" aún no son venta
///    cobrada; entran en el reporte de cartera, y sus abonos en "AbonosRecibidos").
///  - Los rangos son días de calendario de Colombia; se traducen a UTC con <see cref="HoraColombia"/>.
///  - Las sumas y agrupaciones se hacen en SQL cuando son simples y en memoria cuando cruzan tablas
///    sin navegación (el modelo relaciona por Id), igual que DashboardService.
/// </summary>
public class ReporteService : IReporteService
{
    private readonly AppDbContext _db;

    public ReporteService(AppDbContext db)
    {
        _db = db;
    }

    private static (DateTime InicioUtc, DateTime FinUtc) RangoUtc(DateOnly desde, DateOnly hasta) =>
        (HoraColombia.AUtc(desde.ToDateTime(TimeOnly.MinValue)),
         HoraColombia.AUtc(hasta.AddDays(1).ToDateTime(TimeOnly.MinValue)));

    public async Task<ReporteVentasResponse> VentasAsync(DateOnly desde, DateOnly hasta)
    {
        var (inicio, fin) = RangoUtc(desde, hasta);

        var ventas = await _db.Ventas.AsNoTracking()
            .Where(v => v.Estado == "Pagada" && !v.EsCotizacion && v.Fecha >= inicio && v.Fecha < fin)
            .Select(v => new { v.Id, v.Fecha, v.Total, v.MetodoPago })
            .ToListAsync();

        var idsMixtas = ventas.Where(v => v.MetodoPago == "Mixto").Select(v => v.Id).ToList();
        var lineasMixtas = await _db.DetallesPagoVenta.AsNoTracking()
            .Where(d => idsMixtas.Contains(d.VentaId))
            .Select(d => new { d.VentaId, d.MetodoPago, d.Monto })
            .ToListAsync();

        var totalVendido = ventas.Sum(v => v.Total);
        var porDia = ventas
            .GroupBy(v => DateOnly.FromDateTime(HoraColombia.ALocal(v.Fecha)))
            .OrderBy(g => g.Key)
            .Select(g => new VentasPorDiaItem(g.Key, g.Count(), g.Sum(v => v.Total)))
            .ToList();

        // Pagos simples por su método; los "Mixto" se reparten según su desglose.
        var pagosSimples = ventas
            .Where(v => v.MetodoPago != "Mixto")
            .Select(v => (Metodo: v.MetodoPago ?? "Sin método", VentaId: v.Id, Monto: v.Total));
        var pagosMixtos = lineasMixtas.Select(l => (Metodo: l.MetodoPago, VentaId: l.VentaId, Monto: l.Monto));
        var porMetodo = pagosSimples.Concat(pagosMixtos)
            .GroupBy(p => p.Metodo)
            .Select(g => new VentasPorMetodoItem(g.Key, g.Select(p => p.VentaId).Distinct().Count(), g.Sum(p => p.Monto)))
            .OrderByDescending(m => m.Total)
            .ToList();

        var abonosRecibidos = await _db.AbonosCuenta.AsNoTracking()
            .Where(a => a.Fecha >= inicio && a.Fecha < fin)
            .SumAsync(a => (decimal?)a.Monto) ?? 0m;

        return new ReporteVentasResponse(
            desde,
            hasta,
            totalVendido,
            ventas.Count,
            ventas.Count == 0 ? 0m : Math.Round(totalVendido / ventas.Count, 0),
            abonosRecibidos,
            porDia,
            porMetodo);
    }

    public async Task<ReporteProductosVendidosResponse> ProductosVendidosAsync(DateOnly desde, DateOnly hasta, int top)
    {
        var (inicio, fin) = RangoUtc(desde, hasta);

        var vendidos = await _db.DetallesVenta.AsNoTracking()
            .Join(
                _db.Ventas.AsNoTracking()
                    .Where(v => v.Estado == "Pagada" && !v.EsCotizacion && v.Fecha >= inicio && v.Fecha < fin),
                d => d.VentaId,
                v => v.Id,
                (d, v) => new { d.ProductoId, d.Cantidad, d.PrecioUnitario })
            .GroupBy(x => x.ProductoId)
            .Select(g => new
            {
                ProductoId = g.Key,
                Unidades = g.Sum(x => x.Cantidad),
                Ingresos = g.Sum(x => x.Cantidad * x.PrecioUnitario)
            })
            .OrderByDescending(x => x.Ingresos)
            .Take(top)
            .ToListAsync();

        var ids = vendidos.Select(x => x.ProductoId).ToList();
        var productos = await _db.Productos.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.Nombre, p.CategoriaId, p.Costo })
            .ToListAsync();
        var categorias = await _db.Categorias.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Nombre);
        var porId = productos.ToDictionary(p => p.Id);

        // La utilidad usa el costo ACTUAL del producto (la venta no guarda el costo del momento),
        // por eso se llama "estimada".
        var items = vendidos
            .Where(x => porId.ContainsKey(x.ProductoId))
            .Select(x =>
            {
                var p = porId[x.ProductoId];
                return new ProductoVendidoItem(
                    p.Id,
                    p.Nombre,
                    categorias.GetValueOrDefault(p.CategoriaId, "Sin categoría"),
                    x.Unidades,
                    x.Ingresos,
                    x.Ingresos - x.Unidades * p.Costo);
            })
            .ToList();

        return new ReporteProductosVendidosResponse(desde, hasta, items);
    }

    public async Task<ReporteInventarioResponse> InventarioAsync()
    {
        var productos = await _db.Productos.AsNoTracking()
            .Where(p => p.Activo)
            .Select(p => new { p.Id, p.Nombre, p.CategoriaId, p.StockActual, p.StockMinimo, p.Costo, p.Precio })
            .ToListAsync();
        var categorias = await _db.Categorias.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Nombre);
        string NombreCategoria(int id) => categorias.GetValueOrDefault(id, "Sin categoría");

        var porCategoria = productos
            .GroupBy(p => NombreCategoria(p.CategoriaId))
            .Select(g => new InventarioCategoriaItem(
                g.Key,
                g.Count(),
                g.Sum(p => p.StockActual),
                g.Sum(p => p.StockActual * p.Costo),
                g.Sum(p => p.StockActual * p.Precio)))
            .OrderByDescending(c => c.ValorCosto)
            .ToList();

        var stockBajo = productos
            .Where(p => p.StockActual <= p.StockMinimo)
            .OrderBy(p => p.StockActual - p.StockMinimo)
            .ThenBy(p => p.Nombre)
            .Select(p => new ProductoStockBajoItem(
                p.Id, p.Nombre, NombreCategoria(p.CategoriaId), p.StockActual, p.StockMinimo,
                Math.Max(p.StockMinimo - p.StockActual, 0)))
            .ToList();

        return new ReporteInventarioResponse(
            productos.Count,
            productos.Count(p => p.StockActual <= 0),
            stockBajo.Count,
            productos.Sum(p => p.StockActual * p.Costo),
            productos.Sum(p => p.StockActual * p.Precio),
            porCategoria,
            stockBajo);
    }

    public async Task<ReporteCarteraResponse> CarteraAsync()
    {
        var hoy = HoraColombia.Hoy();

        // Por cobrar: cuentas abiertas (fiado) con cliente; saldo = total menos abonos.
        var abiertas = await _db.Ventas.AsNoTracking()
            .Where(v => v.Estado == "Pendiente" && !v.EsCotizacion && v.ClienteId != null)
            .Select(v => new { v.Id, v.NombreComprador, v.Fecha, v.Total })
            .ToListAsync();
        var idsAbiertas = abiertas.Select(v => v.Id).ToList();
        var abonosVenta = await _db.AbonosCuenta.AsNoTracking()
            .Where(a => idsAbiertas.Contains(a.VentaId))
            .GroupBy(a => a.VentaId)
            .Select(g => new { VentaId = g.Key, Abonado = g.Sum(a => a.Monto) })
            .ToDictionaryAsync(x => x.VentaId, x => x.Abonado);

        var porCobrar = abiertas
            .Select(v => new CuentaPorCobrarItem(
                v.Id,
                v.NombreComprador ?? "Cliente sin nombre registrado",
                v.Fecha,
                Math.Max((hoy - HoraColombia.ALocal(v.Fecha).Date).Days, 0),
                v.Total,
                Math.Max(v.Total - abonosVenta.GetValueOrDefault(v.Id, 0m), 0m)))
            .Where(c => c.Saldo > 0)
            .OrderByDescending(c => c.DiasAbierta)
            .ToList();

        var antiguedad = new[]
        {
            ("0 a 7 días", 0, 7),
            ("8 a 30 días", 8, 30),
            ("Más de 30 días", 31, int.MaxValue),
        }.Select(r => new RangoAntiguedadItem(
            r.Item1,
            porCobrar.Count(c => c.DiasAbierta >= r.Item2 && c.DiasAbierta <= r.Item3),
            porCobrar.Where(c => c.DiasAbierta >= r.Item2 && c.DiasAbierta <= r.Item3).Sum(c => c.Saldo)))
            .ToList();

        // Por pagar: cuentas pendientes agrupadas por proveedor.
        var cuentas = await _db.CuentasPorPagar.AsNoTracking()
            .Where(c => c.Estado == "Pendiente")
            .Select(c => new { c.Id, c.ProveedorId, c.MontoTotal, c.FechaVencimiento })
            .ToListAsync();
        var idsCuentas = cuentas.Select(c => c.Id).ToList();
        var abonosCuenta = await _db.AbonosCuentaPorPagar.AsNoTracking()
            .Where(a => idsCuentas.Contains(a.CuentaPorPagarId))
            .GroupBy(a => a.CuentaPorPagarId)
            .Select(g => new { CuentaId = g.Key, Abonado = g.Sum(a => a.Monto) })
            .ToDictionaryAsync(x => x.CuentaId, x => x.Abonado);
        var proveedores = await _db.Proveedores.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Nombre);

        var conSaldo = cuentas
            .Select(c => new
            {
                c.ProveedorId,
                c.FechaVencimiento,
                Saldo = Math.Max(c.MontoTotal - abonosCuenta.GetValueOrDefault(c.Id, 0m), 0m)
            })
            .Where(c => c.Saldo > 0)
            .ToList();

        var porPagar = conSaldo
            .GroupBy(c => c.ProveedorId)
            .Select(g => new SaldoProveedorItem(
                g.Key,
                proveedores.GetValueOrDefault(g.Key, "Proveedor desconocido"),
                g.Count(),
                g.Sum(c => c.Saldo),
                g.Where(c => c.FechaVencimiento.Date < hoy).Sum(c => c.Saldo),
                g.Min(c => c.FechaVencimiento)))
            .OrderByDescending(p => p.SaldoVencido)
            .ThenByDescending(p => p.Saldo)
            .ToList();

        return new ReporteCarteraResponse(
            porCobrar.Sum(c => c.Saldo),
            antiguedad,
            porCobrar,
            porPagar.Sum(p => p.Saldo),
            porPagar.Sum(p => p.SaldoVencido),
            porPagar);
    }
}
