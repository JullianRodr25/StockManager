using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface IDashboardService
{
    Task<DashboardResumenResponse> ObtenerResumenAsync();
}
