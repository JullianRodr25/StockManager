using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface ITrabajadorService
{
    Task<IReadOnlyList<TrabajadorResponse>> ListarAsync(bool incluirInactivos);
    Task<TrabajadorResponse> ObtenerAsync(int id);
    Task<TrabajadorResponse> CrearAsync(GuardarTrabajadorRequest request);
    Task<TrabajadorResponse> ActualizarAsync(int id, GuardarTrabajadorRequest request);
    Task<TrabajadorResponse> CambiarEstadoAsync(int id, bool activo);
}
