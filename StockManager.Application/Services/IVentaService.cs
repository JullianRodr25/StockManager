using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface IVentaService
{
    Task<VentaResponse> RegistrarVentaAsync(RegistrarVentaRequest request, int empleadoId);
}
