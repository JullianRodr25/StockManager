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
/// A diferencia de WhatsAppNotificationBackgroundService (que reacciona a eventos de una
/// transacción de negocio), este servicio es quien los origina: revisa periódicamente las
/// CuentasPorPagar Pendientes y publica un CuentaPorPagarProximaAVencerEvent por cada una
/// que entre en la ventana de aviso (o ya esté vencida) y no se le haya avisado hoy todavía.
/// El envío real del WhatsApp lo sigue haciendo el dispatcher existente — este servicio solo
/// decide "a quién avisar" y deja constancia (FechaUltimaAlerta) para no repetir el aviso.
/// </summary>
public class CuentasPorPagarVencimientoCheckService : BackgroundService
{
    private static readonly TimeSpan IntervaloEntreChequeos = TimeSpan.FromHours(24);

    private readonly IServiceProvider _serviceProvider;
    private readonly IEventoNotificacionPublisher _eventoPublisher;
    private readonly IOptions<WhatsAppOptions> _opciones;
    private readonly ILogger<CuentasPorPagarVencimientoCheckService> _logger;

    public CuentasPorPagarVencimientoCheckService(
        IServiceProvider serviceProvider,
        IEventoNotificacionPublisher eventoPublisher,
        IOptions<WhatsAppOptions> opciones,
        ILogger<CuentasPorPagarVencimientoCheckService> logger)
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
                _logger.LogError(ex, "Error revisando cuentas por pagar próximas a vencer");
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
        var limite = hoy.AddDays(_opciones.Value.DiasAvisoVencimientoProveedores);

        // Pendiente + dentro de la ventana de aviso (o ya vencida, lo cual siempre cae acá
        // porque su FechaVencimiento < hoy <= limite) + no avisada todavía hoy.
        var cuentas = await db.CuentasPorPagar
            .Where(c => c.Estado == "Pendiente"
                        && c.FechaVencimiento <= limite
                        && (c.FechaUltimaAlerta == null || c.FechaUltimaAlerta.Value.Date < hoy))
            .ToListAsync(ct);

        if (cuentas.Count == 0)
            return;

        foreach (var cuenta in cuentas)
        {
            cuenta.RegistrarAlertaEnviada();
            _eventoPublisher.Publicar(new CuentaPorPagarProximaAVencerEvent(cuenta.Id));
        }

        await db.SaveChangesAsync(ct);
    }
}
