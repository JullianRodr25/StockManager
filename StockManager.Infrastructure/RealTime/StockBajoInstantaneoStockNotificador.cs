using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Events;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.RealTime;

/// <summary>
/// Decora el IStockNotificador real (SignalR) para, además de avisar a las pantallas
/// conectadas, revisar al instante si alguno de los productos cuyo stock acaba de cambiar
/// debe entrar o salir de la alerta de "stock bajo" de la campana del panel.
///
/// Se engancha acá — y no en cada sitio que vende/repone/ajusta stock (VentaService,
/// PedidoService, ProductoService) — porque este es el único punto por el que pasa todo
/// cambio de StockActual ya confirmado en la base de datos, sin importar su origen; así la
/// alerta se siente "en vivo" (como el resto del sistema) sin depender de que alguien se
/// acuerde de llamarla desde cada operación nueva que toque stock a futuro.
///
/// Esta clase nunca llama a Twilio: crea el registro en NotificacionesInternas (campana) y
/// publica un StockBajoAdminEvent, que WhatsAppNotificationBackgroundService convierte en un
/// WhatsApp al admin solo si WhatsApp:Habilitado está activo.
/// </summary>
public class StockBajoInstantaneoStockNotificador : IStockNotificador
{
    private readonly IStockNotificador _interno;
    private readonly AppDbContext _dbContext;
    private readonly INotificacionInternaService _notificaciones;
    private readonly IEventoNotificacionPublisher _eventoPublisher;
    private readonly ILogger<StockBajoInstantaneoStockNotificador> _logger;

    public StockBajoInstantaneoStockNotificador(
        IStockNotificador interno,
        AppDbContext dbContext,
        INotificacionInternaService notificaciones,
        IEventoNotificacionPublisher eventoPublisher,
        ILogger<StockBajoInstantaneoStockNotificador> logger)
    {
        _interno = interno;
        _dbContext = dbContext;
        _notificaciones = notificaciones;
        _eventoPublisher = eventoPublisher;
        _logger = logger;
    }

    public async Task NotificarCambiosAsync(IReadOnlyCollection<CambioStockDto> cambios)
    {
        await _interno.NotificarCambiosAsync(cambios);

        // Mejor esfuerzo: un fallo acá (ej. la base de datos momentáneamente no responde)
        // nunca debe afectar la operación de negocio que ya se guardó y ya avisó por
        // SignalR. El chequeo periódico (NotificacionesStockBajoCheckService) queda como
        // respaldo si esto llega a fallar.
        try
        {
            await VerificarStockBajoAsync(cambios);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revisando stock bajo instantáneo tras un cambio de stock");
        }
    }

    private async Task VerificarStockBajoAsync(IReadOnlyCollection<CambioStockDto> cambios)
    {
        var idsProductos = cambios.Select(c => c.ProductoId).Distinct().ToList();
        if (idsProductos.Count == 0) return;

        // El mismo AppDbContext ya tiene trackeados (con su StockActual recién guardado) los
        // productos que acaban de cambiar — EF los devuelve del identity map sin ir a la base
        // de datos de nuevo en ese caso, así que esto es prácticamente gratis.
        var productos = await _dbContext.Productos
            .Where(p => idsProductos.Contains(p.Id) && p.Activo)
            .ToListAsync();

        if (productos.Count == 0) return;

        var huboCambios = false;

        foreach (var producto in productos)
        {
            var resultado = StockBajoEvaluador.Evaluar(
                producto.StockActual, producto.StockMinimo, producto.NotificacionStockBajoActiva);

            if (resultado.DebeNotificar)
            {
                producto.MarcarNotificacionStockBajoActiva();
                huboCambios = true;

                await _notificaciones.CrearAsync(
                    "StockBajo",
                    $"Stock bajo: {producto.Nombre}",
                    $"Quedan {producto.StockActual} unidad(es) (mínimo {producto.StockMinimo}).",
                    "Producto",
                    producto.Id);

                // Mismo momento (una vez por episodio): aviso por WhatsApp al admin, si está habilitado.
                _eventoPublisher.Publicar(new StockBajoAdminEvent(producto.Id));
            }
            else if (resultado.DebeLimpiarBandera)
            {
                producto.LimpiarNotificacionStockBajo();
                huboCambios = true;
            }
        }

        if (huboCambios)
            await _dbContext.SaveChangesAsync();
    }
}
