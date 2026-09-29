using System.Threading.Channels;
using StockManager.Application.Services;
using StockManager.Domain.Events;

namespace StockManager.Infrastructure.Notificaciones;

/// <summary>
/// Implementación de IEventoNotificacionPublisher sobre un Channel en memoria (sin
/// dependencias externas tipo message broker: para el volumen de una sola ferretería
/// es más que suficiente, y sigue siendo un límite claro entre "negocio" y "envío").
/// El canal es ilimitado a propósito: Publicar() nunca debe bloquear ni fallar por
/// contrapresión, ya que se llama desde el mismo hilo que atiende la petición HTTP.
/// </summary>
public class ChannelEventoNotificacionPublisher : IEventoNotificacionPublisher
{
    private readonly Channel<DomainEvent> _canal;

    public ChannelEventoNotificacionPublisher(Channel<DomainEvent> canal)
    {
        _canal = canal;
    }

    public void Publicar(DomainEvent evento)
    {
        // TryWrite sobre un canal Unbounded siempre devuelve true salvo que el canal ya
        // se haya completado (proceso apagándose); si eso pasa, el evento simplemente se
        // pierde en vez de lanzar una excepción que rompería la operación de negocio.
        _canal.Writer.TryWrite(evento);
    }
}
