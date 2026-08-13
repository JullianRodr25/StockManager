using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface IVentaService
{
    Task<VentaResponse> RegistrarVentaAsync(RegistrarVentaRequest request, int empleadoId);

    Task<(List<VentaResumenResponse> Items, int Total)> ObtenerVentasPaginadoAsync(
        int pagina, int tamanoPagina, DateTime? desde, DateTime? hasta, string? estado);

    Task<VentaResponse?> ObtenerVentaPorIdAsync(int id);
}
