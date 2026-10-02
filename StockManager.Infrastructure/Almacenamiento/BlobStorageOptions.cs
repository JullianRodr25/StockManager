namespace StockManager.Infrastructure.Almacenamiento;

/// <summary>
/// Configuración de Azure Blob Storage para archivos subidos por los usuarios (hoy: fotos de
/// perfil de Cliente). Se vincula a la sección "BlobStorage" de appsettings.json.
///
/// Igual que con Email/WhatsApp, el valor real de ConnectionString NUNCA va en
/// appsettings.json (queda vacío ahí) — se configura como variable de entorno en Azure App
/// Service (ConnectionStrings o Application Settings), nunca en un VITE_* del frontend ni en
/// ningún archivo versionado.
/// </summary>
public class BlobStorageOptions
{
    /// <summary>
    /// Cadena de conexión de la cuenta de Azure Storage (del Azure Portal: Storage Account →
    /// "Claves de acceso"). Se valida al resolver AzureBlobStorageService, no acá, para que
    /// el resto de la aplicación arranque igual aunque esta funcionalidad no esté configurada.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del contenedor donde se guardan las fotos de perfil. Se crea automáticamente
    /// (con acceso público de solo lectura a nivel de blob) la primera vez que se sube un
    /// archivo si todavía no existe.
    /// </summary>
    public string ContainerName { get; set; } = "fotos-clientes";
}
