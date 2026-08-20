using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface IClienteService
{
    Task<List<ClienteResponse>> BuscarClientesAsync(string? busqueda);

    Task<ClienteResponse?> ObtenerClientePorIdAsync(int id);
}
