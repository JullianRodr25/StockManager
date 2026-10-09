using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

/// <summary>
/// Construye el <see cref="FacturaDocumentoResponse"/> (lo que se imprime en la factura) a
/// partir de la Configuración del negocio, la venta o el pedido, el cliente y el vendedor.
/// </summary>
public interface IFacturaDocumentoService
{
    /// <summary>Por Id de factura (lo usa el PDF). Null si no existe o no tiene número.</summary>
    Task<FacturaDocumentoResponse?> ObtenerPorFacturaIdAsync(int facturaId);

    /// <summary>Por Id de venta (lo usa el panel web). Null si la venta no tiene factura.</summary>
    Task<FacturaDocumentoResponse?> ObtenerPorVentaIdAsync(int ventaId);
}
