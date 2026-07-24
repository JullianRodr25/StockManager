using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace StockManager.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de categorías.
/// </summary>
public class CategoriaService : ICategoriaService
{
    private readonly AppDbContext _dbContext;

    public CategoriaService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Obtiene todas las categorías ordenadas por nombre ascendente.
    /// </summary>
    public async Task<List<CategoriaResponse>> ObtenerTodasAsync()
    {
        return await _dbContext.Categorias
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaResponse
            {
                Id = c.Id,
                Nombre = c.Nombre
            })
            .ToListAsync();
    }

    /// <summary>
    /// Crea una nueva categoría con validaciones.
    /// </summary>
    /// <exception cref="CategoriaDuplicadaException">Si la categoría ya existe (case-insensitive)</exception>
    public async Task<CategoriaResponse> CrearAsync(CrearCategoriaRequest request)
    {
        // Normalizar el nombre
        var nombreNormalizado = request.Nombre.Trim();

        // Validar que no exista una categoría con ese nombre (case-insensitive)
        var categoriaExistente = await _dbContext.Categorias
            .FirstOrDefaultAsync(c => c.Nombre.ToUpper() == nombreNormalizado.ToUpper());

        if (categoriaExistente != null)
            throw new CategoriaDuplicadaException(nombreNormalizado);

        // Crear la categoría usando el factory method del dominio
        var categoria = Categoria.Crear(nombreNormalizado);

        // Guardar en la base de datos
        _dbContext.Categorias.Add(categoria);
        await _dbContext.SaveChangesAsync();

        // Devolver el DTO con la categoría creada (incluyendo el Id generado)
        return new CategoriaResponse
        {
            Id = categoria.Id,
            Nombre = categoria.Nombre
        };
    }
}
