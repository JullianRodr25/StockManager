using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Infrastructure.Data;
using StockManager.Infrastructure.Notificaciones;

namespace StockManager.Infrastructure.Services;

/// <summary>
/// Agrega estadísticas de varias áreas del negocio (ventas, inventario, pedidos, cuentas por
/// pagar, fiados de clientes) para el Dashboard del panel. Es de solo lectura: no muta nada,
/// así que todas las consultas usan AsNoTracking.
/// </summary>
public class DashboardService : IDashboardService
{
    // Mismo criterio de "pedido activo" que ClienteService usa para bloquear la desactivación
    // de un cliente — un pedido en cualquiera de estos estados todavía requiere atención.
    private static readonly string[] EstadosPedidoActivos = { "Pendiente", "Confirmado", "EnPreparacion", "EnCamino" };

    private readonly AppDbContext _dbContext;
    private readonly IOptions<WhatsAppOptions> _opciones;

    public DashboardService(AppDbContext dbContext, IOptions<WhatsAppOptions> opciones)
    {
        _dbContext = dbContext;
        _opciones = opciones;
    }

    public async Task<DashboardResumenResponse> ObtenerResumenAsync()
    {
        var hoy = DateTime.UtcNow.Date;
        var manana = hoy.AddDays(1);
        // Misma ventana de "próxima a vencer" que ya usa CuentasPorPagarVencimientoCheckService,
        // para que esta tarjeta del Dashboard sea consistente con la notificación de la campana.
        var limiteVencimiento = hoy.AddDays(_opciones.Value.DiasAvisoVencimientoProveedores);

        var ventasHoyQuery = _dbContext.Ventas.AsNoTracking()
            .Where(v => v.Fecha >= hoy && v.Fecha < manana && v.Estado != "Cancelada");
        var ventasHoyTotal = await ventasHoyQuery.SumAsync(v => (decimal?)v.Total) ?? 0m;
        var ventasHoyCantidad = await ventasHoyQuery.CountAsync();

        var productosStockBajo = await _dbContext.Productos.AsNoTracking()
            .CountAsync(p => p.Activo && p.StockActual <= p.StockMinimo);

        var pedidosActivos = await _dbContext.Pedidos.AsNoTracking()
            .CountAsync(p => EstadosPedidoActivos.Contains(p.Estado));

        var (cuentasPorPagarPorVencer, totalPorPagarProveedores) =
            await CalcularCuentasPorPagarAsync(limiteVencimiento);

        var (clientesConFiadoAbierto, totalPorCobrarFiado) = await CalcularFiadosAbiertosAsync();

        var clientesActivos = await _dbContext.Clientes.AsNoTracking().CountAsync(c => c.Activo);

        var actividadReciente = await ConstruirActividadRecienteAsync();

        return new DashboardResumenResponse(
            ventasHoyTotal,
            ventasHoyCantidad,
            productosStockBajo,
            pedidosActivos,
            cuentasPorPagarPorVencer,
            totalPorPagarProveedores,
            totalPorCobrarFiado,
            clientesConFiadoAbierto,
            clientesActivos,
            actividadReciente);
    }

    /// <summary>
    /// El saldo pendiente de una cuenta por pagar no es una columna almacenada (MontoTotal
    /// menos sus abonos), así que se trae la lista de cuentas Pendientes y sus abonos por
    /// separado y se combina en memoria — el mismo patrón que usa
    /// CuentaPorPagarService.MapearResumenesAsync, para no repetir la resta en SQL con un
    /// subquery correlacionado.
    /// </summary>
    private async Task<(int PorVencer, decimal TotalPendiente)> CalcularCuentasPorPagarAsync(DateTime limiteVencimiento)
    {
        var cuentasPendientes = await _dbContext.CuentasPorPagar.AsNoTracking()
            .Where(c => c.Estado == "Pendiente")
            .Select(c => new { c.Id, c.MontoTotal, c.FechaVencimiento })
            .ToListAsync();

        if (cuentasPendientes.Count == 0)
            return (0, 0m);

        var idsCuentas = cuentasPendientes.Select(c => c.Id).ToList();
        var abonosPorCuenta = await _dbContext.AbonosCuentaPorPagar.AsNoTracking()
            .Where(a => idsCuentas.Contains(a.CuentaPorPagarId))
            .GroupBy(a => a.CuentaPorPagarId)
            .Select(g => new { CuentaPorPagarId = g.Key, TotalAbonado = g.Sum(a => a.Monto) })
            .ToDictionaryAsync(x => x.CuentaPorPagarId, x => x.TotalAbonado);

        var porVencer = cuentasPendientes.Count(c => c.FechaVencimiento <= limiteVencimiento);
        var totalPendiente = cuentasPendientes.Sum(c => c.MontoTotal - abonosPorCuenta.GetValueOrDefault(c.Id, 0m));

        return (porVencer, totalPendiente);
    }

    /// <summary>
    /// Un fiado abierto es una Venta en estado "Pendiente" asociada a un Cliente — el mismo
    /// criterio exacto que usa VentaService.AbrirFiadoAsync para impedir que un cliente abra
    /// una segunda cuenta mientras ya tiene una activa.
    /// </summary>
    private async Task<(int Cantidad, decimal TotalPendiente)> CalcularFiadosAbiertosAsync()
    {
        var fiadosAbiertos = await _dbContext.Ventas.AsNoTracking()
            .Where(v => v.Estado == "Pendiente" && v.ClienteId != null)
            .Select(v => new { v.Id, v.Total })
            .ToListAsync();

        if (fiadosAbiertos.Count == 0)
            return (0, 0m);

        var idsFiados = fiadosAbiertos.Select(v => v.Id).ToList();
        var abonosPorFiado = await _dbContext.AbonosCuenta.AsNoTracking()
            .Where(a => idsFiados.Contains(a.VentaId))
            .GroupBy(a => a.VentaId)
            .Select(g => new { VentaId = g.Key, TotalAbonado = g.Sum(a => a.Monto) })
            .ToDictionaryAsync(x => x.VentaId, x => x.TotalAbonado);

        var totalPendiente = fiadosAbiertos.Sum(v => v.Total - abonosPorFiado.GetValueOrDefault(v.Id, 0m));

        return (fiadosAbiertos.Count, totalPendiente);
    }

    /// <summary>
    /// Combina las ventas, pedidos y notificaciones internas más recientes en un solo feed
    /// cronológico. Se trae un puñado de cada fuente y se ordena/recorta en memoria — más
    /// simple y suficientemente rápido para el volumen de datos de esta app que un UNION en SQL.
    /// </summary>
    private async Task<List<ActividadRecienteResponse>> ConstruirActividadRecienteAsync()
    {
        const int cantidadPorFuente = 6;
        const int cantidadTotal = 8;

        var ventasRecientes = await _dbContext.Ventas.AsNoTracking()
            .OrderByDescending(v => v.Fecha)
            .Take(cantidadPorFuente)
            .Select(v => new { v.Fecha, v.Total, v.NombreComprador, v.Estado })
            .ToListAsync();

        var pedidosRecientes = await _dbContext.Pedidos.AsNoTracking()
            .Join(
                _dbContext.Clientes.AsNoTracking(),
                pedido => pedido.ClienteId,
                cliente => cliente.Id,
                (pedido, cliente) => new { Pedido = pedido, ClienteNombre = cliente.Nombre })
            .OrderByDescending(x => x.Pedido.Fecha)
            .Take(cantidadPorFuente)
            .Select(x => new { x.Pedido.Id, x.Pedido.Fecha, x.Pedido.Estado, x.Pedido.Total, x.ClienteNombre })
            .ToListAsync();

        var notificacionesRecientes = await _dbContext.NotificacionesInternas.AsNoTracking()
            .OrderByDescending(n => n.FechaCreacion)
            .Take(cantidadPorFuente)
            .Select(n => new { n.FechaCreacion, n.Titulo })
            .ToListAsync();

        var items = new List<ActividadRecienteResponse>(cantidadPorFuente * 3);

        items.AddRange(ventasRecientes.Select(v => new ActividadRecienteResponse(
            v.Estado == "Pendiente"
                ? $"Cuenta fiada abierta — {v.NombreComprador} — ${v.Total:N0}"
                : $"Venta registrada — {v.NombreComprador ?? "cliente de mostrador"} — ${v.Total:N0}",
            v.Fecha)));

        items.AddRange(pedidosRecientes.Select(p => new ActividadRecienteResponse(
            $"Pedido #{p.Id} ({p.Estado}) — {p.ClienteNombre} — ${p.Total:N0}",
            p.Fecha)));

        items.AddRange(notificacionesRecientes.Select(n => new ActividadRecienteResponse(n.Titulo, n.FechaCreacion)));

        return items
            .OrderByDescending(i => i.Fecha)
            .Take(cantidadTotal)
            .ToList();
    }
}
