using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

/// <summary>
/// Lee un Excel de productos (el de la plantilla o el exportado) y crea o edita productos.
/// </summary>
public interface IProductoExcelImportador
{
    /// <summary>
    /// Procesa el archivo completo. La pasada es la misma con o sin <paramref name="aplicarCambios"/>:
    /// solo cambia si al final se guardan los cambios. Así la vista previa muestra exactamente lo
    /// que ocurriría al confirmar.
    ///
    /// Todo o nada: si CUALQUIER fila tiene un error no se guarda nada. Si no hay errores y
    /// <paramref name="aplicarCambios"/> es true, todo se guarda en una sola transacción.
    /// </summary>
    /// <param name="archivo">Contenido del .xlsx</param>
    /// <param name="aplicarCambios">false = solo validar y devolver el resumen; true = validar y guardar</param>
    /// <exception cref="InvalidOperationException">El archivo no se puede leer o no tiene el formato esperado</exception>
    Task<ImportarProductosResponse> ProcesarAsync(Stream archivo, bool aplicarCambios);
}
