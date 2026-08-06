using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

/// <summary>
/// Interfaz para el servicio de gestión de productos.
/// Define operaciones para listar, obtener, crear, importar y gestionar etiquetas.
/// </summary>
public interface IProductoService
{
    /// <summary>
    /// Obtiene un producto por su ID.
    /// </summary>
    Task<ProductoResponse?> ObtenerProductoPorIdAsync(int id);

    /// <summary>
    /// Obtiene un producto por su código de barras.
    /// </summary>
    Task<ProductoResponse?> ObtenerProductoPorCodigoBarrasAsync(string codigoBarras);

    /// <summary>
    /// Obtiene una lista paginada de productos.
    /// </summary>
    /// <param name="pagina">Número de página (1-based)</param>
    /// <param name="tamanoPagina">Cantidad de items por página</param>
    /// <param name="categoriaId">ID de categoría opcional para filtrar</param>
    Task<(List<ProductoResponse> Items, int Total)> ObtenerProductosPaginadoAsync(int pagina, int tamanoPagina, int? categoriaId = null);

    /// <summary>
    /// Obtiene el catálogo público paginado de productos activos para clientes.
    /// </summary>
    Task<(List<ProductoCatalogoResponse> Items, int Total)> ObtenerCatalogoPaginadoAsync(int pagina, int tamanoPagina, int? categoriaId);

    /// <summary>
    /// Crea un nuevo producto.
    /// Si CodigoBarras viene vacío, lo genera automáticamente.
    /// </summary>
    Task<ProductoResponse> CrearProductoAsync(CrearProductoRequest request);

    /// <summary>
    /// Actualiza un producto existente.
    /// No modifica StockActual, solo información general (nombre, categoría, precio, stock mínimo, código de barras).
    /// </summary>
    Task<ProductoResponse> ActualizarProductoAsync(int id, ActualizarProductoRequest request);

    /// <summary>
    /// Desactiva un producto (eliminación lógica).
    /// </summary>
    Task DesactivarProductoAsync(int id);

    /// <summary>
    /// Reactiva un producto previamente desactivado.
    /// </summary>
    Task ReactivarProductoAsync(int id);

    /// <summary>
    /// Importa productos masivamente desde un archivo Excel.
    /// Continúa con las siguientes filas incluso si algunas fallan.
    /// </summary>
    /// <param name="archivo">El archivo Excel a importar</param>
    Task<ImportarProductosResponse> ImportarProductosDesdeExcelAsync(Stream archivoStream);

    /// <summary>
    /// Obtiene todos los productos que tienen etiquetas pendientes de imprimir.
    /// Filtra por EsCodigoGenerado = true AND FechaImpresionEtiqueta IS NULL.
    /// </summary>
    Task<List<EtiquetaPendienteResponse>> ObtenerEtiquetasPendientesAsync();

    /// <summary>
    /// Genera imágenes de códigos de barras en base64 para una lista de productos.
    /// Marca FechaImpresionEtiqueta = ahora para cada uno.
    /// </summary>
    /// <param name="productosIds">IDs de productos a generar etiquetas</param>
    Task<List<EtiquetaGeneradaResponse>> GenerarEtiquetasAsync(List<int> productosIds);
}
