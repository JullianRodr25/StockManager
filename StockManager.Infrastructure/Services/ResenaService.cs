using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de reseñas de producto.
/// </summary>
public class ResenaService : IResenaService
{
    private readonly AppDbContext _dbContext;

    public ResenaService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ResenaResponse>> ListarResenasAsync(int productoId, int clienteActualId)
    {
        var resenas = await _dbContext.ResenasProducto.AsNoTracking()
            .Where(r => r.ProductoId == productoId)
            .Join(
                _dbContext.Clientes.AsNoTracking(),
                resena => resena.ClienteId,
                cliente => cliente.Id,
                (resena, cliente) => new { Resena = resena, ClienteNombre = cliente.Nombre })
            .OrderByDescending(x => x.Resena.FechaCreacion)
            .ToListAsync();

        return resenas
            .Select(x => MapearResponse(x.Resena, x.ClienteNombre, clienteActualId))
            .ToList();
    }

    public async Task<ResenaResponse> CrearResenaAsync(int productoId, int clienteId, CrearResenaRequest request)
    {
        var producto = await _dbContext.Productos.FirstOrDefaultAsync(p => p.Id == productoId);
        if (producto == null || !producto.Activo)
            throw new ProductoNoEncontradoException(productoId);

        var yaReseno = await _dbContext.ResenasProducto
            .AnyAsync(r => r.ProductoId == productoId && r.ClienteId == clienteId);
        if (yaReseno)
            throw new ResenaDuplicadaException(productoId, clienteId);

        var resena = ResenaProducto.Crear(productoId, clienteId, request.Calificacion, request.Comentario);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        _dbContext.ResenasProducto.Add(resena);
        await _dbContext.SaveChangesAsync();

        await RecalcularAgregadosAsync(producto, productoId);

        await transaction.CommitAsync();

        var clienteNombre = await _dbContext.Clientes.AsNoTracking()
            .Where(c => c.Id == clienteId)
            .Select(c => c.Nombre)
            .FirstAsync();

        return MapearResponse(resena, clienteNombre, clienteId);
    }

    public async Task<ResenaResponse> EditarResenaAsync(int resenaId, int clienteId, EditarResenaRequest request)
    {
        var resena = await ObtenerPropiaOLanzarAsync(resenaId, clienteId);

        resena.Editar(request.Calificacion, request.Comentario);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        await _dbContext.SaveChangesAsync();

        var producto = await _dbContext.Productos.FirstAsync(p => p.Id == resena.ProductoId);
        await RecalcularAgregadosAsync(producto, resena.ProductoId);

        await transaction.CommitAsync();

        var clienteNombre = await _dbContext.Clientes.AsNoTracking()
            .Where(c => c.Id == clienteId)
            .Select(c => c.Nombre)
            .FirstAsync();

        return MapearResponse(resena, clienteNombre, clienteId);
    }

    public async Task EliminarResenaAsync(int resenaId, int clienteId)
    {
        var resena = await ObtenerPropiaOLanzarAsync(resenaId, clienteId);
        var productoId = resena.ProductoId;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        _dbContext.ResenasProducto.Remove(resena);
        await _dbContext.SaveChangesAsync();

        var producto = await _dbContext.Productos.FirstAsync(p => p.Id == productoId);
        await RecalcularAgregadosAsync(producto, productoId);

        await transaction.CommitAsync();
    }

    /// <summary>
    /// Busca una reseña por Id y valida que pertenezca al cliente que la está editando/
    /// borrando. Si no existe o es de otro cliente, lanza la misma ResenaNoEncontradaException
    /// (con el mismo mensaje genérico) para no filtrarle a un cliente si una reseña ajena
    /// existe o no.
    /// </summary>
    private async Task<ResenaProducto> ObtenerPropiaOLanzarAsync(int resenaId, int clienteId)
    {
        var resena = await _dbContext.ResenasProducto
            .FirstOrDefaultAsync(r => r.Id == resenaId && r.ClienteId == clienteId);

        if (resena == null)
            throw new ResenaNoEncontradaException(resenaId);

        return resena;
    }

    /// <summary>
    /// Recalcula CalificacionPromedio/TotalResenas de un producto desde cero, a partir de
    /// ResenasProducto — siempre dentro de la misma transacción que la escritura de la reseña
    /// que lo disparó, para que dos reseñas casi simultáneas del mismo producto no se pisen el
    /// promedio entre sí (se relee el agregado fresco desde SQL en vez de calcularlo en
    /// memoria con datos que pudieron quedar desactualizados).
    /// </summary>
    private async Task RecalcularAgregadosAsync(Producto producto, int productoId)
    {
        var stats = await _dbContext.ResenasProducto
            .Where(r => r.ProductoId == productoId)
            .GroupBy(r => 1)
            .Select(g => new { Promedio = g.Average(r => (decimal)r.Calificacion), Total = g.Count() })
            .FirstOrDefaultAsync();

        producto.ActualizarCalificacion(
            stats == null ? null : Math.Round(stats.Promedio, 2),
            stats?.Total ?? 0);

        await _dbContext.SaveChangesAsync();
    }

    private static ResenaResponse MapearResponse(ResenaProducto resena, string clienteNombre, int clienteActualId) =>
        new(
            resena.Id,
            resena.ClienteId,
            clienteNombre,
            resena.Calificacion,
            resena.Comentario,
            resena.FechaCreacion,
            resena.FechaEdicion,
            resena.ClienteId == clienteActualId);
}
