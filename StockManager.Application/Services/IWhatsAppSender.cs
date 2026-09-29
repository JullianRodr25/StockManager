namespace StockManager.Application.Services;

/// <summary>
/// Resultado de un intento de envío por WhatsApp. Nunca lanza excepción por un fallo del
/// proveedor (número inválido, credenciales, red, etc.) — el llamador decide qué hacer
/// con el resultado (típicamente, registrarlo en NotificacionLog).
/// </summary>
public record ResultadoEnvioWhatsApp(bool Exitoso, string? Error);

/// <summary>
/// Puerto de salida hacia el proveedor de WhatsApp (hoy: Twilio). Mantener esta interfaz
/// en Application, separada de la implementación en Infrastructure, permite cambiar de
/// proveedor (por ejemplo, a la API directa de Meta) sin tocar quién la consume.
/// </summary>
public interface IWhatsAppSender
{
    Task<ResultadoEnvioWhatsApp> EnviarTextoAsync(string telefonoDestino, string mensaje);

    /// <summary>
    /// Envía un mensaje con un documento adjunto. <paramref name="urlDocumento"/> debe ser
    /// una URL pública (el proveedor la descarga él mismo) — no se sube el archivo directo.
    /// </summary>
    Task<ResultadoEnvioWhatsApp> EnviarDocumentoAsync(string telefonoDestino, string mensaje, string urlDocumento);
}
