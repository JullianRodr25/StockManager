using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface INominaResumenService
{
    Task<NominaResumenResponse> ObtenerAsync();
}
