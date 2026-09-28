using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface IPedidoService
{
    /// <summary>
    /// Crea un pedido a domicilio desde la PWA. Por cada línea con stock suficiente se
    /// descuenta el stock de inmediato (reserva) y queda 'Disponible'; si no hay stock
    /// suficiente, la línea queda 'PorEncargo' y se genera un BackorderRequest.
    /// </summary>
    Task<PedidoResponse> CrearPedidoAsync(CrearPedidoRequest request, int clienteId);

    Task<(List<PedidoResumenResponse> Items, int Total)> ObtenerPedidosPaginadoAsync(
        int pagina, int tamanoPagina, string? estado, int? clienteId);

    Task<PedidoResponse?> ObtenerPedidoPorIdAsync(int id);

    Task<PedidoResponse> ConfirmarAsync(int pedidoId);

    Task<PedidoResponse> IniciarPreparacionAsync(int pedidoId);

    Task<PedidoResponse> EnviarACaminoAsync(int pedidoId);

    /// <summary>
    /// Marca el pedido como entregado: genera la Venta y la Factura equivalentes (mismas
    /// líneas y total) para que quede contabilizado igual que cualquier otra venta.
    /// </summary>
    Task<PedidoResponse> MarcarEntregadoAsync(int pedidoId, string metodoPago, int empleadoId);

    /// <summary>
    /// Cancela el pedido y repone el stock de sus líneas 'Disponible' (las 'PorEncargo'
    /// nunca descontaron stock). También cancela los BackorderRequest pendientes asociados.
    /// </summary>
    Task<PedidoResponse> CancelarAsync(int pedidoId);
}
