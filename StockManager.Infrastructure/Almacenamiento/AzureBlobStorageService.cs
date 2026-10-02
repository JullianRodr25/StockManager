using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using StockManager.Application.Services;

namespace StockManager.Infrastructure.Almacenamiento;

/// <summary>
/// Implementación de IBlobStorageService sobre Azure Blob Storage. El contenedor se crea con
/// acceso público de solo lectura a nivel de blob (PublicAccessType.Blob): cualquiera con la
/// URL puede ver la foto (es lo esperado para un avatar mostrado en la UI), pero no puede
/// listar el contenedor ni ver otros archivos sin conocer su nombre exacto.
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobStorageService(IOptions<BlobStorageOptions> opciones)
    {
        var config = opciones.Value;

        if (string.IsNullOrWhiteSpace(config.ConnectionString))
            throw new InvalidOperationException(
                "BlobStorage:ConnectionString no está configurada. Agrégala en la configuración de Azure App " +
                "Service (nunca en appsettings.json ni en un VITE_* del frontend).");

        if (string.IsNullOrWhiteSpace(config.ContainerName))
            throw new InvalidOperationException("BlobStorage:ContainerName no está configurado.");

        var serviceClient = new BlobServiceClient(config.ConnectionString);
        _containerClient = serviceClient.GetBlobContainerClient(config.ContainerName);
    }

    public async Task<string> SubirArchivoAsync(
        Stream contenido,
        string nombreBlob,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await _containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blobClient = _containerClient.GetBlobClient(nombreBlob);
        await blobClient.UploadAsync(
            contenido,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);

        return blobClient.Uri.ToString();
    }

    public async Task EliminarArchivoAsync(string url, CancellationToken cancellationToken = default)
    {
        var nombreBlob = ExtraerNombreBlobDesdeUrl(url);
        if (nombreBlob == null)
            return;

        var blobClient = _containerClient.GetBlobClient(nombreBlob);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Recupera el path del blob a partir de su URL pública, ej.
    /// "https://cuenta.blob.core.windows.net/fotos-clientes/clientes/42/abc.jpg" se convierte
    /// en "clientes/42/abc.jpg". Devuelve null si la URL no pertenece a este contenedor
    /// (defensivo: evita borrar a ciegas algo que no subimos nosotros).
    /// </summary>
    private string? ExtraerNombreBlobDesdeUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var prefijo = $"/{_containerClient.Name}/";
        var indice = uri.AbsolutePath.IndexOf(prefijo, StringComparison.OrdinalIgnoreCase);
        if (indice < 0)
            return null;

        return Uri.UnescapeDataString(uri.AbsolutePath[(indice + prefijo.Length)..]);
    }
}
