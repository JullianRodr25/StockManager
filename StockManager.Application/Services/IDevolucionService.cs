using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

/// <summary>
/// Devoluciones y cambios sobre ventas ya pagadas. La venta original nunca se modifica: cada
/// devolución es un documento nuevo ligado a ella.
/// </summary>
public interface IDevolucionService
{
    /// <summary>
    /// Registra una devolución, con o sin cambio, en una sola operación atómica: devuelve el stock,
    /// entrega lo del cambio, y deja registrado el dinero que entra o sale de la caja. Si algo
    /// falla (stock insuficiente en el cambio, sin caja abierta para un reembolso en efectivo)
    /// no se guarda nada.
    /// </summary>
    /// <exception cref="StockManager.Domain.Exceptions.VentaNoEncontradaException">La venta no existe.</exception>
    /// <exception cref="StockManager.Domain.Exceptions.VentaEstadoInvalidoException">La venta no está pagada.</exception>
    /// <exception cref="StockManager.Domain.Exceptions.TurnoCajaNoAbiertoException">Hay efectivo de por medio y la caja no está abierta.</exception>
    Task<DevolucionResponse> RegistrarAsync(int ventaId, RegistrarDevolucionRequest request, int empleadoId);

    /// <summary>Devoluciones de una venta, de la más reciente a la más antigua.</summary>
    Task<List<DevolucionResponse>> ObtenerPorVentaAsync(int ventaId);
}
