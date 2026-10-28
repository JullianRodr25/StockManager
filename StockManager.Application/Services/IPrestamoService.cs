using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface IPrestamoService
{
    Task<IReadOnlyList<PrestamoResponse>> ListarAsync(string? estado, int? trabajadorId);
    Task<PrestamoResponse> CrearAsync(CrearPrestamoRequest request);
    Task<PrestamoResponse> CancelarAsync(int id);
}
