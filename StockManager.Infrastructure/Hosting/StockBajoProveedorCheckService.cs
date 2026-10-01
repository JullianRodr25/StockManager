using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockManager.Application.Services;
using StockManager.Domain.Events;
using StockManager.Infrastructure.Data;
using StockManager.Infrastructure.Notificaciones;

namespace StockManager.Infrastructure.Hosting;

/// <summary>
/// Revisa periódicamente qué productos activos con Proveedor asignado están en stock bajo
/// (StockActual &lt;= StockMinimo, condición derivada, no persistida) y publica un
/// StockBajoProveedorEvent por cada Proveedor afectado que no se le haya avisado todavía hoy.
/// El envío real del WhatsApp lo hace WhatsAppNotificationBackgroundService — este servicio
/// solo decide "a qué proveedor avisar" y deja constancia (FechaUltimaAlertaStockBajo) para
/// no repetir el aviso el mismo día. Sigue el mismo patrón que
/// CuentasPorPagarVencimientoCheckService.
/// </summary>
public class StockBajoProveedorCheckService : BackgroundService
{
    private static readonly TimeSpan IntervaloEntreChequeos = TimeSpan.FromHours(24);

    private readonly IServiceProvider _serviceProvider;
    private readonly IEventoNotificacionPublisher _eventoPublisher;
    private readonly IOptions<WhatsAppOptions> _opciones;
    private readonly ILogger<StockBajoProveedorCheckService> _logger;

    public StockBajoProveedorCheckService(
        IServiceProvider serviceProvider,
        IEventoNotificacionPublisher eventoPublisher,
        IOptions<WhatsAppOptions> opciones,
        ILogger<StockBajoProveedorCheckService> logger)
    {
        _serviceProvider = serviceProvider;
        _eventoPublisher = eventoPublisher;
        _opciones = opciones;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(IntervaloEntreChequeos);

        // Un chequeo inmediato al arrancar la aplicación, y luego uno cada 24 horas.
        do
        {
            try
            {
                await EjecutarChequeoAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revisando productos en stock bajo con proveedor asignado");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task EjecutarChequeoAsync(CancellationToken ct)
    {
        if (!_opciones.Value.Habilitado)
            return;

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var hoy = DateTime.UtcNow.Date;

        // IDs de los proveedores con al menos un producto activo en stock bajo.
        var proveedorIdsConStockBajo = await db.Productos
            .Where(p => p.Activo && p.ProveedorId != null && p.StockActual <= p.StockMinimo)
            .Select(p => p.ProveedorId!.Value)
            .Distinct()
            .ToListAsync(ct);

        if (proveedorIdsConStockBajo.Count == 0)
            return;

        // De esos, solo los proveedores activos, con NumeroWhatsApp configurado y que no
        // hayan sido avisados todavía hoy (mismo criterio que CuentasPorPagarVencimientoCheckService).
        var proveedores = await db.Proveedores
            .Where(p => proveedorIdsConStockBajo.Contains(p.Id)
                        && p.Activo
                        && p.NumeroWhatsApp != null
                        && (p.FechaUltimaAlertaStockBajo == null || p.FechaUltimaAlertaStockBajo.Value.Date < hoy))
            .ToListAsync(ct);

        if (proveedores.Count == 0)
            return;

        foreach (var proveedor in proveedores)
        {
            proveedor.RegistrarAlertaStockBajoEnviada();
            _eventoPublisher.Publicar(new StockBajoProveedorEvent(proveedor.Id));
        }

        await db.SaveChangesAsync(ct);
    }
}
