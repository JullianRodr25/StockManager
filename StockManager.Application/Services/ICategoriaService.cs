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

    /// <summary>
    /// Cambia el nombre de una categoría. Los productos la siguen usando (se ligan por Id).
    /// </summary>
    /// <exception cref="StockManager.Domain.Exceptions.CategoriaNoEncontradaException">Si no existe.</exception>
    /// <exception cref="StockManager.Domain.Exceptions.CategoriaDuplicadaException">Si otra categoría ya tiene ese nombre.</exception>
    Task<CategoriaResponse> ActualizarAsync(int id, ActualizarCategoriaRequest request);

    /// <summary>
    /// Elimina una categoría. Solo se permite si ningún producto (activo o inactivo) la usa.
    /// </summary>
    /// <exception cref="StockManager.Domain.Exceptions.CategoriaNoEncontradaException">Si no existe.</exception>
    /// <exception cref="StockManager.Domain.Exceptions.CategoriaConProductosException">Si todavía tiene productos.</exception>
    Task EliminarAsync(int id);
}
