namespace StockManager.Infrastructure.Notificaciones;

/// <summary>
/// Credenciales y configuración de la cuenta de Twilio usada para enviar mensajes de
/// WhatsApp. Se vincula a la sección "WhatsApp:Twilio" de appsettings.json.
///
/// Para probar en el Sandbox de WhatsApp de Twilio (gratis, sin verificación de negocio):
/// 1. Crear una cuenta en https://www.twilio.com/try-twilio
/// 2. En la consola, ir a Messaging → Try it out → Send a WhatsApp message, y seguir las
///    instrucciones para unir tu propio número al sandbox (se envía un código por WhatsApp
///    al número que Twilio indica, algo como "whatsapp:+14155238886").
/// 3. Copiar AccountSid y AuthToken desde el dashboard principal de la consola.
/// 4. FromWhatsAppNumber es el número del sandbox (con prefijo "whatsapp:"), no un número propio.
/// </summary>
public class TwilioOptions
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>Número remitente, con el prefijo "whatsapp:" incluido (ej. "whatsapp:+14155238886").</summary>
    public string FromWhatsAppNumber { get; set; } = string.Empty;
}
