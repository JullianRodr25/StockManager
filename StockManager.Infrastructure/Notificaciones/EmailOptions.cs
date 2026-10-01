namespace StockManager.Infrastructure.Notificaciones;

/// <summary>
/// Configuración general de la funcionalidad de envío de correos (independiente del
/// proveedor concreto — ver BrevoOptions para las credenciales de Brevo).
/// Se vincula a la sección "Email" de appsettings.json.
///
/// Hoy el único correo transaccional que envía el sistema es el de recuperación de
/// contraseña (Empleados y Clientes), pero esta clase queda separada de ese caso de uso
/// (igual que WhatsAppOptions no sabe de facturas ni de cuentas por pagar) para poder
/// sumar otros correos más adelante sin reestructurar la configuración.
/// </summary>
public class EmailOptions
{
    /// <summary>
    /// Interruptor general. Si es false, IEmailSender descarta los envíos sin intentar
    /// contactar al proveedor — útil para desarrollo local sin credenciales de Brevo.
    /// </summary>
    public bool Habilitado { get; set; } = false;

    /// <summary>Nombre del remitente que verá el destinatario (ej. "Ferretería Gold").</summary>
    public string RemitenteNombre { get; set; } = string.Empty;

    /// <summary>
    /// Correo remitente. Debe ser un remitente verificado en la cuenta de Brevo (o un
    /// dominio verificado) para que los correos no terminen en spam o sean rechazados.
    /// </summary>
    public string RemitenteEmail { get; set; } = string.Empty;

    /// <summary>
    /// URL base del panel interno (stockmanager-web), usada para construir el link de
    /// restablecimiento de contraseña que reciben los Empleados (ej. "https://panel.miferreteria.com").
    /// Sin "/" al final.
    /// </summary>
    public string FrontendBaseUrlPanel { get; set; } = string.Empty;

    /// <summary>
    /// URL base de la tienda para clientes (stockmanager-pwa), usada para construir el link
    /// de restablecimiento de contraseña que reciben los Clientes (ej. "https://tienda.miferreteria.com").
    /// Sin "/" al final.
    /// </summary>
    public string FrontendBaseUrlPwa { get; set; } = string.Empty;

    /// <summary>
    /// Minutos de vigencia del link firmado de restablecimiento de contraseña.
    /// </summary>
    public int RecuperacionVigenciaMinutos { get; set; } = 30;

    /// <summary>
    /// Clave secreta usada para firmar (HMAC-SHA256) el token de un solo propósito que
    /// autoriza restablecer una contraseña puntual. Debe ser un valor largo y aleatorio,
    /// distinto por ambiente, y nunca compartirse fuera del backend.
    /// </summary>
    public string RecuperacionSecret { get; set; } = string.Empty;
}
