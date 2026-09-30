using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

/// <summary>
/// Puerto de salida para avisar en tiempo real que el stock de uno o más productos cambió
/// (venta, edición/cancelación de una línea, pedido reservando o liberando stock, etc.).
/// Mantener esta interfaz en Application, separada de la implementación en Infrastructure
/// (hoy: un Hub de SignalR), permite cambiar el transporte de tiempo real sin tocar a quien
/// lo consume (VentaService, PedidoService).
///
/// Es "mejor esfuerzo" a propósito: un fallo al avisar (ej. el Hub no tiene clientes, o el
/// WebSocket falló) nunca debe tumbar la operación de negocio que lo dispara. El propio
/// llamador es responsable de invocarlo dentro de un try/catch después de guardar los
/// cambios, nunca antes — solo tiene sentido avisar de un cambio que ya quedó persistido.
/// </summary>
public interface IStockNotificador
{
    Task NotificarCambiosAsync(IReadOnlyCollection<CambioStockDto> cambios);
}
