using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface ICotizacionService
{
    Task<CotizacionResponse> CrearAsync(GuardarCotizacionRequest request, int empleadoId, bool esAdmin);

    /// <summary>Edita una cotización Vigente (aunque haya vencido: editar el plazo la reactiva).</summary>
    Task<CotizacionResponse> ActualizarAsync(int id, GuardarCotizacionRequest request, bool esAdmin);

    Task<CotizacionResponse> ObtenerAsync(int id);

    /// <summary>
    /// Listado paginado. estado: Vigente, Vencida, Convertida o Anulada (vacío = todas);
    /// busqueda: nombre del cliente o número (COT-00012 / 12).
    /// </summary>
    Task<(List<CotizacionResumenResponse> Items, int Total)> ListarAsync(
        int pagina, int tamanoPagina, string? estado, string? busqueda, DateOnly? desde, DateOnly? hasta);

    Task<CotizacionResponse> AnularAsync(int id);

    /// <summary>PDF de la cotización para entregar al cliente.</summary>
    Task<byte[]> GenerarPdfAsync(int id);
}
