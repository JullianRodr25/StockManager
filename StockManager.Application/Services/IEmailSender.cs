namespace StockManager.Application.Services;

/// <summary>
/// Resultado de un intento de envío de correo. Nunca lanza excepción por un fallo del
/// proveedor (credenciales, red, remitente no verificado, etc.) — el llamador decide qué
/// hacer con el resultado. Igual que ResultadoEnvioWhatsApp.
/// </summary>
public record ResultadoEnvioEmail(bool Exitoso, string? Error);

/// <summary>
/// Puerto de salida hacia el proveedor de correo transaccional (hoy: Brevo). Mantener esta
/// interfaz en Application, separada de la implementación en Infrastructure, permite cambiar
/// de proveedor sin tocar quién la consume (a diferencia de WhatsApp, acá no hay plantillas
/// aprobadas por un tercero: el HTML del correo lo arma el propio backend).
/// </summary>
public interface IEmailSender
{
    Task<ResultadoEnvioEmail> EnviarAsync(string destinatarioEmail, string destinatarioNombre, string asunto, string htmlContenido);
}
