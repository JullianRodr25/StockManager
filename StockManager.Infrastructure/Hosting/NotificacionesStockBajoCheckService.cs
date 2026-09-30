using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockManager.Application.Services;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Hosting;

/// <summary>
/// Revisa periódicamente todos los productos activos y genera (o limpia) la notificación
/// interna de "stock bajo" en la campana del panel. A diferencia de
/// StockBajoProveedorCheckService (que solo mira productos con Proveedor asignado y
/// WhatsApp configurado, para avisarle al proveedor), este chequeo es general — cualquier
/// producto activo en stock bajo genera un aviso interno para el personal — y es
/// independiente de WhatsApp:Habilitado, porque no envía nada por WhatsApp.
///
/// Producto.NotificacionStockBajoActiva evita reabrir la misma notificación mientras el
/// stock sigue bajo: solo se genera una vez por "episodio" (hasta que se repone por encima
/// del mínimo), en línea con que el usuario decidió que cerrar una notificación no debe
/// hacer que reaparezca sola mientras la condición siga igual.
/// </summary>
public class NotificacionesStockBajoCheckService : BackgroundService
{
    // Más frecuente que los chequeos diarios (cuentas por pagar, stock bajo a proveedores)
    // porque esto solo alimenta un badge en el panel, no envía nada externo: es barato
    // revisarlo seguido y le da al personal una alerta razonablemente oportuna.
    private static readonly TimeSpan IntervaloEntreChequeos = TimeSpan.FromHours(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificacionesStockBajoCheckService> _logger;

    public NotificacionesStockBajoCheckService(IServiceProvider serviceProvider, ILogger<NotificacionesStockBajoCheckService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(IntervaloEntreChequeos);

        do
        {
            try
            {
                await EjecutarChequeoAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revisando notificaciones de stock bajo");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task EjecutarChequeoAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notificaciones = scope.ServiceProvider.GetRequiredService<INotificacionInternaService>();

        // Solo los productos cuyo estado de alerta puede necesitar cambiar: los que están en
        // stock bajo ahora mismo, o los que ya tenían la bandera activa (para poder limpiarla
        // si se repusieron).
        var productos = await db.Productos
            .Where(p => p.Activo && (p.StockActual <= p.StockMinimo || p.NotificacionStockBajoActiva))
            .ToListAsync(ct);

        if (productos.Count == 0)
            return;

        var huboAlertaNueva = false;

        foreach (var producto in productos)
        {
            var enStockBajo = producto.StockActual <= producto.StockMinimo;

            if (enStockBajo && !producto.NotificacionStockBajoActiva)
            {
                producto.MarcarNotificacionStockBajoActiva();
                huboAlertaNueva = true;

                await notificaciones.CrearAsync(
                    "StockBajo",
                    $"Stock bajo: {producto.Nombre}",
                    $"Quedan {producto.StockActual} unidad(es) (mínimo {producto.StockMinimo}).",
                    "Producto",
                    producto.Id);
            }
            else if (!enStockBajo && producto.NotificacionStockBajoActiva)
            {
                producto.LimpiarNotificacionStockBajo();
            }
        }

        if (huboAlertaNueva)
            _logger.LogInformation("Se generaron nuevas notificaciones de stock bajo");

        await db.SaveChangesAsync(ct);
    }
}
