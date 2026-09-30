using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface IProveedorService
{
    Task<ProveedorResponse> CrearAsync(CrearProveedorRequest request);
    Task<ProveedorResponse> ActualizarAsync(int id, ActualizarProveedorRequest request);
    Task<ProveedorResponse> DesactivarAsync(int id);
    Task<ProveedorResponse> ActivarAsync(int id);
    Task<ProveedorResponse?> ObtenerPorIdAsync(int id);
    Task<(List<ProveedorResponse> Items, int Total)> ObtenerPaginadoAsync(int pagina, int tamanoPagina, bool? activo);
}
