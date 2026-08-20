using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface IVentaService
{
    Task<VentaResponse> RegistrarVentaAsync(RegistrarVentaRequest request, int empleadoId);

    Task<(List<VentaResumenResponse> Items, int Total)> ObtenerVentasPaginadoAsync(
        int pagina, int tamanoPagina, DateTime? desde, DateTime? hasta, string? estado);

    Task<VentaResponse?> ObtenerVentaPorIdAsync(int id);

    Task<VentaResponse> AbrirFiadoAsync(int clienteId, int empleadoId);

    Task<VentaResponse> AgregarLineaFiadoAsync(int ventaId, LineaVentaRequest linea);

    Task<VentaResponse> CerrarFiadoAsync(int ventaId, string metodoPago);
}
