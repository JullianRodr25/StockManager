using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Common;
using StockManager.Domain.Entities;
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

        // Devoluciones y cambios del período: lo que movieron en dinero (cambio − devuelto) se suma a
        // las ventas, en el día y con el método en que se saldó. Así los totales son netos.
        var devoluciones = await _db.Devoluciones.AsNoTracking()
            .Where(d => d.Fecha >= inicio && d.Fecha < fin)
            .Select(d => new { d.Fecha, d.MetodoPago, Neto = d.TotalCambio - d.TotalDevuelto })
            .ToListAsync();

        var totalVendido = ventas.Sum(v => v.Total) + devoluciones.Sum(d => d.Neto);
        var porDia = ventas
            .GroupBy(v => DateOnly.FromDateTime(HoraColombia.ALocal(v.Fecha)))
            .Select(g => (Dia: g.Key, Cantidad: g.Count(), Total: g.Sum(v => v.Total)))
            .Concat(devoluciones
                .GroupBy(d => DateOnly.FromDateTime(HoraColombia.ALocal(d.Fecha)))
                .Select(g => (Dia: g.Key, Cantidad: 0, Total: g.Sum(d => d.Neto))))
            .GroupBy(x => x.Dia)
            .OrderBy(g => g.Key)
            .Select(g => new VentasPorDiaItem(g.Key, g.Sum(x => x.Cantidad), g.Sum(x => x.Total)))
            .ToList();

        // Pagos simples por su método; los "Mixto" se reparten según su desglose. Las devoluciones
        // con dinero de por medio entran con su método y sin VentaId (no cuentan como una venta más).
        var pagosSimples = ventas
            .Where(v => v.MetodoPago != "Mixto")
            .Select(v => (Metodo: v.MetodoPago ?? "Sin método", VentaId: (int?)v.Id, Monto: v.Total));
        var pagosMixtos = lineasMixtas.Select(l => (Metodo: l.MetodoPago, VentaId: (int?)l.VentaId, Monto: l.Monto));
        var pagosDevolucion = devoluciones
            .Where(d => d.MetodoPago != null && d.Neto != 0)
            .Select(d => (Metodo: d.MetodoPago!, VentaId: (int?)null, Monto: d.Neto));
        var porMetodo = pagosSimples.Concat(pagosMixtos).Concat(pagosDevolucion)
            .GroupBy(p => p.Metodo)
            .Select(g => new VentasPorMetodoItem(
                g.Key,
                g.Where(p => p.VentaId.HasValue).Select(p => p.VentaId!.Value).Distinct().Count(),
                g.Sum(p => p.Monto)))
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

    public async Task<ReporteCotizacionesResponse> CotizacionesAsync(DateOnly desde, DateOnly hasta)
    {
        var (inicio, fin) = RangoUtc(desde, hasta);
        var hoy = DateOnly.FromDateTime(HoraColombia.Hoy());

        var filas = await _db.Cotizaciones.AsNoTracking()
            .Where(c => c.Fecha >= inicio && c.Fecha < fin)
            .Select(c => new
            {
                c.Id, c.NombreCliente, c.EmpleadoId, c.Fecha, c.FechaVencimiento, c.Estado, c.Total,
                c.VentaId, Lineas = c.Detalles.Count
            })
            .ToListAsync();

        var empleadoIds = filas.Select(f => f.EmpleadoId).Distinct().ToList();
        var nombres = await _db.Empleados.AsNoTracking()
            .Where(e => empleadoIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Nombre);

        // "Vencida" se deduce: sigue Vigente y su plazo ya pasó.
        string EstadoDe(string estado, DateOnly vence) =>
            estado == Cotizacion.EstadoVigente && vence < hoy ? Cotizacion.EstadoVencida : estado;

        var items = filas
            .OrderByDescending(f => f.Fecha)
            .Select(f => new CotizacionResumenResponse(
                f.Id,
                CotizacionService.FormatearNumero(f.Id),
                f.NombreCliente,
                nombres.GetValueOrDefault(f.EmpleadoId, string.Empty),
                f.Fecha,
                f.FechaVencimiento,
                EstadoDe(f.Estado, f.FechaVencimiento),
                f.Total,
                f.Lineas,
                f.VentaId))
            .ToList();

        var porEstado = new[] { Cotizacion.EstadoVigente, Cotizacion.EstadoVencida, Cotizacion.EstadoConvertida, Cotizacion.EstadoAnulada }
            .Select(estado => new CotizacionesPorEstadoResponse(
                estado,
                items.Count(i => i.Estado == estado),
                items.Where(i => i.Estado == estado).Sum(i => i.Total)))
            .ToList();

        var porEmpleado = filas
            .GroupBy(f => f.EmpleadoId)
            .Select(g => new CotizacionesPorEmpleadoResponse(
                g.Key,
                nombres.GetValueOrDefault(g.Key, string.Empty),
                g.Count(),
                g.Sum(f => f.Total),
                g.Count(f => f.Estado == Cotizacion.EstadoConvertida),
                g.Where(f => f.Estado == Cotizacion.EstadoConvertida).Sum(f => f.Total)))
            .OrderByDescending(e => e.Valor)
            .ToList();

        var convertidas = items.Where(i => i.Estado == Cotizacion.EstadoConvertida).ToList();
        var competidas = items.Where(i => i.Estado != Cotizacion.EstadoAnulada).ToList();
        var valorCompetido = competidas.Sum(i => i.Total);
        var valorConvertido = convertidas.Sum(i => i.Total);

        return new ReporteCotizacionesResponse(
            desde,
            hasta,
            items.Count,
            items.Sum(i => i.Total),
            valorConvertido,
            competidas.Count == 0 ? 0m : Math.Round(100m * convertidas.Count / competidas.Count, 1),
            valorCompetido == 0m ? 0m : Math.Round(100m * valorConvertido / valorCompetido, 1),
            porEstado,
            porEmpleado,
            items);
    }
}
