namespace StockManager.Application.Services;

/// <summary>
/// Abstracción sobre el proveedor de almacenamiento de archivos (hoy Azure Blob Storage, ver
/// AzureBlobStorageService en Infrastructure). Mantenida separada de IClienteService: subir y
/// borrar archivos es una responsabilidad de infraestructura, distinta de las reglas de
/// negocio sobre Cliente, que solo conoce la URL resultante.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Sube un archivo y devuelve su URL pública. nombreBlob debe incluir cualquier "carpeta"
    /// virtual deseada (ej. "clientes/42/550e8400-....jpg") para mantener el contenedor
    /// organizado y evitar colisiones de nombres entre clientes.
    /// </summary>
    Task<string> SubirArchivoAsync(Stream contenido, string nombreBlob, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un archivo a partir de su URL pública (tal como la devolvió SubirArchivoAsync).
    /// No lanza si el blob ya no existe (operación idempotente).
    /// </summary>
    Task EliminarArchivoAsync(string url, CancellationToken cancellationToken = default);
}
