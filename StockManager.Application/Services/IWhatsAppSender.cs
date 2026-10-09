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
///
/// Solo expone envío por plantilla (Content Template) — nunca texto libre. Cualquier
/// mensaje que este sistema envía es iniciado por el negocio (no es una respuesta dentro de
/// una conversación abierta por el destinatario), así que WhatsApp exige una plantilla
/// aprobada por Meta; un Body de texto libre sería rechazado en producción. Ver
/// TwilioOptions.ContentSidXxx y PLANTILLAS-WHATSAPP.md para el texto de cada plantilla.
/// </summary>
public interface IWhatsAppSender
{
    /// <summary>
    /// Envía una plantilla de WhatsApp aprobada. <paramref name="contentSid"/> identifica la
    /// plantilla (formato "HXxxxxxxxx...", asignado por Twilio al aprobarla). Las
    /// <paramref name="variables"/> llenan los placeholders "{{1}}", "{{2}}", etc. de esa
    /// plantilla — tanto los del cuerpo como los del encabezado, si la plantilla tiene un
    /// encabezado de tipo documento/imagen con una URL dinámica (ej. la factura en PDF).
    /// </summary>
    Task<ResultadoEnvioWhatsApp> EnviarPlantillaAsync(
        string telefonoDestino,
        string contentSid,
        IReadOnlyDictionary<string, string> variables);

    /// <summary>
    /// SOLO PARA PRUEBAS en el sandbox de Twilio: envía texto libre (Body). Funciona únicamente
    /// dentro de las 24 horas posteriores a que el destinatario le escribió al número remitente;
    /// en producción WhatsApp lo rechaza, por eso el sistema solo lo usa cuando
    /// WhatsAppOptions.PermitirTextoLibrePruebas está activo y la plantilla no está configurada.
    /// </summary>
    Task<ResultadoEnvioWhatsApp> EnviarTextoLibreAsync(string telefonoDestino, string texto);
}
