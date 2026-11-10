using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface IVentaService
{
    Task<VentaResponse> RegistrarVentaAsync(RegistrarVentaRequest request, int empleadoId);

    /// <summary>
    /// Convierte una cotización vigente en venta, de forma atómica: crea la venta con las líneas
    /// y los precios COTIZADOS (no los de lista), descuenta el stock y deja la cotización
    /// enlazada a la venta. Falla, sin cambios, si falta stock o la cotización ya no es vigente.
    /// </summary>
    Task<VentaResponse> RegistrarVentaDesdeCotizacionAsync(int cotizacionId, ConvertirCotizacionRequest request, int empleadoId);

    Task<(List<VentaResumenResponse> Items, int Total)> ObtenerVentasPaginadoAsync(
        int pagina, int tamanoPagina, DateTime? desde, DateTime? hasta, string? estado);

    Task<VentaResponse?> ObtenerVentaPorIdAsync(int id);

    Task<VentaResponse> AbrirFiadoAsync(int clienteId, int empleadoId);

    Task<VentaResponse> AgregarLineaFiadoAsync(int ventaId, LineaVentaRequest linea);

    Task<VentaResponse> CerrarFiadoAsync(int ventaId, string metodoPago, int empleadoId, decimal? montoRecibido = null, List<DetallePagoRequest>? detallesPago = null);

    /// <summary>Corrige el método de pago de una venta pagada, solo durante el día en que se registró.</summary>
    Task<VentaResponse> CambiarMetodoPagoAsync(int ventaId, CambiarMetodoPagoRequest request, int empleadoId);

    Task<VentaResponse> RegistrarAbonoAsync(int ventaId, decimal monto, string metodoPago, int empleadoId);

    Task<List<AbonoResponse>> ObtenerAbonosAsync(int ventaId);

    Task<VentaResponse> EditarCantidadLineaAsync(int ventaId, int detalleId, int nuevaCantidad);

    Task<VentaResponse> QuitarLineaAsync(int ventaId, int detalleId);

    Task<VentaResponse> CancelarCuentaAsync(int ventaId);

    /// <summary>
    /// Los últimos N productos distintos vendidos (de más a menos reciente), para los accesos
    /// directos del mostrador en Ventas. Ignora ventas Canceladas y productos inactivos.
    /// </summary>
    Task<List<ProductoVentaRecienteResponse>> ObtenerProductosRecientesAsync(int limite = 10);
}
