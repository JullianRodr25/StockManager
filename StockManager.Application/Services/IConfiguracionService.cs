using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface IConfiguracionService
{
    Task<ConfiguracionResponse> ObtenerAsync();
    Task<ConfiguracionResponse> ActualizarAsync(ActualizarConfiguracionRequest request);
}