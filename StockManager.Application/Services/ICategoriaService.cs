using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

/// <summary>
/// Servicio de aplicación para operaciones CRUD de categorías.
/// </summary>
public interface ICategoriaService
{
    /// <summary>
    /// Obtiene todas las categorías ordenadas por nombre ascendente.
    /// </summary>
    Task<List<CategoriaResponse>> ObtenerTodasAsync();

    /// <summary>
    /// Crea una nueva categoría.
    /// </summary>
    /// <param name="request">Datos de la categoría a crear</param>
    /// <returns>La categoría creada</returns>
    /// <exception cref="StockManager.Domain.Exceptions.CategoriaDuplicadaException">
    /// Se lanza si ya existe una categoría con el mismo nombre (case-insensitive)
    /// </exception>
    Task<CategoriaResponse> CrearAsync(CrearCategoriaRequest request);
}
