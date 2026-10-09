using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

/// <summary>
/// Lee el Excel de terceros del programa contable anterior y crea los clientes. Está separado de
/// <see cref="IClienteService"/> porque leer archivos es una responsabilidad distinta a las
/// reglas de los clientes (que sí se reutilizan, vía la entidad Cliente).
/// </summary>
public interface IClienteExcelImportador
{
    /// <summary>
    /// La pasada es la misma con o sin <paramref name="aplicarCambios"/>; solo cambia si al final
    /// se guarda. Todo o nada: con cualquier error de fila no se guarda nada.
    /// </summary>
    /// <exception cref="InvalidOperationException">El archivo no se puede leer o no tiene el formato esperado</exception>
    Task<ImportarClientesResponse> ProcesarAsync(Stream archivo, bool aplicarCambios);
}
