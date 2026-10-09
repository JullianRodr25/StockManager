namespace StockManager.Application.Services;

/// <summary>
/// Genera los archivos Excel de productos: la plantilla vacía para crear productos nuevos y la
/// exportación del inventario actual. Está separado de <see cref="IProductoService"/> porque
/// producir y leer archivos es una responsabilidad distinta a la de las reglas de los productos.
/// </summary>
public interface IProductoExcelService
{
    /// <summary>
    /// Plantilla vacía con las columnas, listas desplegables (categorías y proveedores vigentes),
    /// límites de validación y una hoja de instrucciones.
    /// </summary>
    Task<byte[]> GenerarPlantillaAsync();

    /// <summary>
    /// Copia del inventario actual (solo productos activos) en el mismo formato de la plantilla,
    /// más las columnas de edición (stock actual, ajuste de stock e Id).
    /// </summary>
    Task<byte[]> ExportarInventarioAsync();
}
