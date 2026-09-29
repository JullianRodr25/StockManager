using StockManager.Domain.Events;

namespace StockManager.Application.Services;

/// <summary>
/// Publica eventos de dominio relacionados con notificaciones (WhatsApp, etc.) para que
/// se procesen de forma asíncrona y desacoplada de la transacción que los originó.
/// La implementación (Infrastructure) los encola; un BackgroundService aparte los consume
/// y hace el envío real. Si el envío falla, la operación de negocio que publicó el evento
/// (confirmar un pedido, cerrar una venta, etc.) ya se completó y no se ve afectada.
/// </summary>
public interface IEventoNotificacionPublisher
{
    void Publicar(DomainEvent evento);
}
