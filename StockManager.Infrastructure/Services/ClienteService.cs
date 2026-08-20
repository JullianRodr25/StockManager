using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

public class ClienteService : IClienteService
{
    private readonly AppDbContext _dbContext;

    public ClienteService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ClienteResponse>> BuscarClientesAsync(string? busqueda)
    {
        var query = _dbContext.Clientes.AsNoTracking().Where(c => c.Activo).AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim().ToUpper();
            query = query.Where(c =>
                c.Nombre.ToUpper().Contains(termino) ||
                c.NumeroIdentificacion.ToUpper().Contains(termino));
        }

        return await query
            .OrderBy(c => c.Nombre)
            .Take(50)
            .Select(c => new ClienteResponse(
                c.Id,
                c.NumeroIdentificacion,
                c.Nombre,
                c.Email,
                c.Telefono,
                c.Direccion,
                c.Activo))
            .ToListAsync();
    }

    public async Task<ClienteResponse?> ObtenerClientePorIdAsync(int id)
    {
        return await _dbContext.Clientes
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ClienteResponse(
                c.Id,
                c.NumeroIdentificacion,
                c.Nombre,
                c.Email,
                c.Telefono,
                c.Direccion,
                c.Activo))
            .FirstOrDefaultAsync();
    }
}
