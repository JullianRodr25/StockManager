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

    /// <summary>
    /// ContentSid (empiezan con "HX...") de las plantillas de WhatsApp aprobadas por Meta,
    /// una por cada "familia" de mensaje que el sistema envía. Fuera de la ventana de 24h de
    /// una conversación (que es el caso normal para todos estos avisos, iniciados por el
    /// sistema y no en respuesta a un mensaje del destinatario), WhatsApp exige que cualquier
    /// mensaje use una de estas plantillas — un Body de texto libre es rechazado por Meta.
    ///
    /// El texto exacto a enviar a aprobación en la consola de Twilio (Content Template
    /// Builder) para cada una está documentado en PLANTILLAS-WHATSAPP.md, en la raíz del
    /// repositorio. Mientras un ContentSid quede vacío, WhatsAppNotificationBackgroundService
    /// no envía ese tipo de mensaje (queda registrado en NotificacionLog como fallido) en vez
    /// de arriesgarse a que Twilio/Meta lo rechace o penalice el número por enviar texto libre
    /// fuera de ventana.
    /// </summary>
    public string? ContentSidPedidoActualizacionCliente { get; set; }
    public string? ContentSidPedidoNuevoAdmin { get; set; }
    public string? ContentSidAlertaCuentaPorPagar { get; set; }
    public string? ContentSidAlertaStockBajoProveedor { get; set; }
    public string? ContentSidAlertaStockBajoAdmin { get; set; }
    public string? ContentSidFacturaCliente { get; set; }
}
