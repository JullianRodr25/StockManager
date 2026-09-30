namespace StockManager.Infrastructure.Notificaciones;

/// <summary>
/// Configuración general de la funcionalidad de notificaciones por WhatsApp (independiente
/// del proveedor concreto — ver TwilioOptions para las credenciales de Twilio).
/// Se vincula a la sección "WhatsApp" de appsettings.json.
///
/// El número de WhatsApp que recibe las alertas administrativas NO vive acá: es
/// Configuracion.TelefonoNotificacionesAdmin (tabla Configuracion, vía IConfiguracionService),
/// para que un Admin pueda cambiarlo desde la pantalla de Configuración sin un despliegue.
/// Esta clase solo agrupa ajustes técnicos/de infraestructura que sí requieren redeploy.
/// </summary>
public class WhatsAppOptions
{
    /// <summary>
    /// Interruptor general. Si es false, el BackgroundService descarta los eventos sin
    /// intentar enviarlos — útil para desarrollo local sin credenciales de Twilio.
    /// </summary>
    public bool Habilitado { get; set; } = false;

    /// <summary>
    /// URL pública base de la API (ej. "https://api.miferreteria.com"), usada para construir
    /// el link que Twilio descarga al enviar el PDF de una factura. Debe ser alcanzable desde
    /// internet, no localhost.
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Minutos de vigencia del link firmado de descarga del PDF de una factura.
    /// </summary>
    public int FacturaLinkVigenciaMinutos { get; set; } = 60;

    /// <summary>
    /// Clave secreta usada para firmar (HMAC-SHA256) el token de un solo propósito que
    /// protege el endpoint público de descarga del PDF de una factura. Debe ser un valor
    /// largo y aleatorio, distinto por ambiente, y nunca compartirse fuera del backend.
    /// </summary>
    public string LinkSecret { get; set; } = string.Empty;

    /// <summary>
    /// Días de anticipación con que se avisa al admin que una CuentaPorPagar está por vencer
    /// (0 = solo el mismo día de vencimiento). Una cuenta ya vencida siempre se avisa,
    /// independientemente de este valor.
    /// </summary>
    public int DiasAvisoVencimientoProveedores { get; set; } = 3;
}
