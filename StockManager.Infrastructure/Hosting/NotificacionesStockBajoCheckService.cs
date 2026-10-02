using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockManager.Application.Services;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Hosting;

/// <summary>
/// Respaldo periódico de la alerta de "stock bajo" en la campana del panel: la vía principal
/// es instantánea (ver StockBajoInstantaneoStockNotificador, que revisa el producto apenas se
/// confirma una venta, un pedido o un ajuste de stock), así que este barrido solo hace falta
/// para los casos que esa vía no cubre — por ejemplo, si se sube el StockMinimo de un
/// producto sin que su stock se mueva, o una importación masiva por Excel que deja productos
/// ya por debajo de su mínimo. A diferencia de StockBajoProveedorCheckService (que solo mira
/// productos con Proveedor asignado y WhatsApp configurado, para avisarle al proveedor), este
/// chequeo es general — cualquier producto activo en stock bajo genera un aviso interno para
/// el personal — y es independiente de WhatsApp:Habilitado, porque no envía nada por WhatsApp.
///
/// Producto.NotificacionStockBajoActiva evita reabrir la misma notificación mientras el
/// stock sigue bajo: solo se genera una vez por "episodio" (hasta que se repone por encima
/// del mínimo), en línea con que el usuario decidió que cerrar una notificación no debe
/// hacer que reaparezca sola mientras la condición siga igual.
/// </summary>
public class NotificacionesStockBajoCheckService : BackgroundService
{
    // Es solo un respaldo (la vía instantánea cubre el caso normal), así que no hace falta
    // revisar tan seguido como antes; se deja en 15 minutos — barato de todos modos — para
    // que los casos borde que no pasan por la vía instantánea (ver el comentario de la clase)
    // no queden sin avisar por mucho tiempo.
    private static readonly TimeSpan IntervaloEntreChequeos = TimeSpan.FromMinutes(15);

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
            var resultado = StockBajoEvaluador.Evaluar(
                producto.StockActual, producto.StockMinimo, producto.NotificacionStockBajoActiva);

            if (resultado.DebeNotificar)
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
            else if (resultado.DebeLimpiarBandera)
            {
                producto.LimpiarNotificacionStockBajo();
            }
        }

        if (huboAlertaNueva)
            _logger.LogInformation("Se generaron nuevas notificaciones de stock bajo");

        await db.SaveChangesAsync(ct);
    }
}
