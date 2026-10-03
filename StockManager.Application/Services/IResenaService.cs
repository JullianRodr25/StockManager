using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

/// <summary>
/// Interfaz para el servicio de reseñas de producto (catálogo público, PWA de clientes).
/// Regla de negocio: un cliente solo puede tener una reseña por producto (ver
/// ResenaProductoConfiguration); CrearResenaAsync lanza InvalidOperationException si el
/// cliente ya reseñó ese producto.
/// </summary>
public interface IResenaService
{
    /// <summary>
    /// Lista las reseñas de un producto, más recientes primero.
    /// </summary>
    /// <param name="productoId">Producto a consultar</param>
    /// <param name="clienteActualId">
    /// Cliente autenticado que hace la petición, para calcular ResenaResponse.EsPropia en cada
    /// item — no filtra el resultado, todas las reseñas publicadas son visibles para cualquier
    /// cliente.
    /// </param>
    Task<List<ResenaResponse>> ListarResenasAsync(int productoId, int clienteActualId);

    /// <summary>
    /// Crea la reseña de un cliente sobre un producto.
    /// </summary>
    /// <exception cref="StockManager.Domain.Exceptions.ProductoNoEncontradoException">Si el producto no existe o está inactivo.</exception>
    /// <exception cref="StockManager.Domain.Exceptions.ResenaDuplicadaException">Si el cliente ya reseñó este producto.</exception>
    /// <exception cref="ArgumentException">Si la calificación o el comentario no son válidos.</exception>
    Task<ResenaResponse> CrearResenaAsync(int productoId, int clienteId, CrearResenaRequest request);

    /// <summary>
    /// Edita una reseña existente. Solo el cliente dueño de la reseña puede editarla.
    /// </summary>
    /// <exception cref="StockManager.Domain.Exceptions.ResenaNoEncontradaException">Si la reseña no existe o no pertenece al cliente.</exception>
    /// <exception cref="ArgumentException">Si la calificación o el comentario no son válidos.</exception>
    Task<ResenaResponse> EditarResenaAsync(int resenaId, int clienteId, EditarResenaRequest request);

    /// <summary>
    /// Elimina una reseña existente. Solo el cliente dueño de la reseña puede eliminarla.
    /// </summary>
    /// <exception cref="StockManager.Domain.Exceptions.ResenaNoEncontradaException">Si la reseña no existe o no pertenece al cliente.</exception>
    Task EliminarResenaAsync(int resenaId, int clienteId);
}
