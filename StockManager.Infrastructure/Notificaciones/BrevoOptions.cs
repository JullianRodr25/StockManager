namespace StockManager.Infrastructure.Notificaciones;

/// <summary>
/// Credenciales de la cuenta de Brevo (antes Sendinblue) usada para enviar correos
/// transaccionales. Se vincula a la sección "Email:Brevo" de appsettings.json.
///
/// Brevo se eligió sobre SendGrid porque mantiene un plan gratuito permanente de 300
/// correos/día sin pedir tarjeta de crédito (el plan gratuito de SendGrid pasó a ser solo
/// una prueba de 60 días). Para obtener la API key:
/// 1. Crear una cuenta gratuita en https://www.brevo.com
/// 2. Verificar el remitente (EmailOptions.RemitenteEmail) en Senders, Domains &amp; Dedicated IPs.
/// 3. En el menú de la cuenta, ir a SMTP &amp; API → API Keys → Generate a new API key.
/// 4. Copiar esa key acá (ApiKey).
/// </summary>
public class BrevoOptions
{
    public string ApiKey { get; set; } = string.Empty;
}
