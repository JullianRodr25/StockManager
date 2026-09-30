using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

public class ProveedorService : IProveedorService
{
    private readonly AppDbContext _dbContext;

    public ProveedorService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProveedorResponse> CrearAsync(CrearProveedorRequest request)
    {
        var proveedor = Proveedor.Crear(
            request.Nombre,
            request.NumeroIdentificacion,
            request.Telefono,
            request.Email,
            request.Direccion);

        _dbContext.Proveedores.Add(proveedor);
        await _dbContext.SaveChangesAsync();

        return MapearResponse(proveedor);
    }

    public async Task<ProveedorResponse> ActualizarAsync(int id, ActualizarProveedorRequest request)
    {
        var proveedor = await _dbContext.Proveedores.FirstOrDefaultAsync(p => p.Id == id);
        if (proveedor == null)
            throw new ProveedorNoEncontradoException(id);

        proveedor.ActualizarInformacion(
            request.Nombre,
            request.NumeroIdentificacion,
            request.Telefono,
            request.Email,
            request.Direccion);

        await _dbContext.SaveChangesAsync();

        return MapearResponse(proveedor);
    }

    public async Task<ProveedorResponse> DesactivarAsync(int id)
    {
        var proveedor = await _dbContext.Proveedores.FirstOrDefaultAsync(p => p.Id == id);
        if (proveedor == null)
            throw new ProveedorNoEncontradoException(id);

        proveedor.Desactivar();
        await _dbContext.SaveChangesAsync();

        return MapearResponse(proveedor);
    }

    public async Task<ProveedorResponse> ActivarAsync(int id)
    {
        var proveedor = await _dbContext.Proveedores.FirstOrDefaultAsync(p => p.Id == id);
        if (proveedor == null)
            throw new ProveedorNoEncontradoException(id);

        proveedor.Activar();
        await _dbContext.SaveChangesAsync();

        return MapearResponse(proveedor);
    }

    public async Task<ProveedorResponse?> ObtenerPorIdAsync(int id)
    {
        var proveedor = await _dbContext.Proveedores.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return proveedor == null ? null : MapearResponse(proveedor);
    }

    public async Task<(List<ProveedorResponse> Items, int Total)> ObtenerPaginadoAsync(int pagina, int tamanoPagina, bool? activo)
    {
        var query = _dbContext.Proveedores.AsNoTracking().AsQueryable();

        if (activo.HasValue)
            query = query.Where(p => p.Activo == activo.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Nombre)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return (items.Select(MapearResponse).ToList(), total);
    }

    private static ProveedorResponse MapearResponse(Proveedor proveedor) => new(
        proveedor.Id,
        proveedor.Nombre,
        proveedor.NumeroIdentificacion,
        proveedor.Telefono,
        proveedor.Email,
        proveedor.Direccion,
        proveedor.Activo);
}
