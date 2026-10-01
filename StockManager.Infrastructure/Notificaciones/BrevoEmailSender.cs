using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockManager.Application.Services;

namespace StockManager.Infrastructure.Notificaciones;

/// <summary>
/// Implementación de IEmailSender sobre la API REST transaccional de Brevo
/// (POST https://api.brevo.com/v3/smtp/email), sin SDK — igual que TwilioWhatsAppSender, la
/// superficie usada es un único endpoint JSON y no amerita una dependencia pesada. Se
/// registra con AddHttpClient&lt;IEmailSender, BrevoEmailSender&gt;() para reutilizar el
/// HttpClient.
///
/// Nunca lanza excepción por un fallo del envío: todo se traduce a
/// ResultadoEnvioEmail(false, "..."), porque el llamador (AuthService, en el flujo de
/// recuperación de contraseña) no debe fallar la petición HTTP del usuario por un problema
/// del proveedor de correo — es un envío de "mejor esfuerzo".
/// </summary>
public class BrevoEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly EmailOptions _emailOpciones;
    private readonly BrevoOptions _brevoOpciones;
    private readonly ILogger<BrevoEmailSender> _logger;

    public BrevoEmailSender(
        HttpClient httpClient,
        IOptions<EmailOptions> emailOpciones,
        IOptions<BrevoOptions> brevoOpciones,
        ILogger<BrevoEmailSender> logger)
    {
        _httpClient = httpClient;
        _emailOpciones = emailOpciones.Value;
        _brevoOpciones = brevoOpciones.Value;
        _logger = logger;
    }

    public async Task<ResultadoEnvioEmail> EnviarAsync(string destinatarioEmail, string destinatarioNombre, string asunto, string htmlContenido)
    {
        if (!_emailOpciones.Habilitado)
            return new ResultadoEnvioEmail(false, "Envío de correo deshabilitado (Email:Habilitado = false).");

        if (string.IsNullOrWhiteSpace(_brevoOpciones.ApiKey))
            return new ResultadoEnvioEmail(false, "Credenciales de Brevo no configuradas.");

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "v3/smtp/email");
            request.Headers.Add("api-key", _brevoOpciones.ApiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var cuerpo = new
            {
                sender = new { name = _emailOpciones.RemitenteNombre, email = _emailOpciones.RemitenteEmail },
                to = new[] { new { email = destinatarioEmail, name = destinatarioNombre } },
                subject = asunto,
                htmlContent = htmlContenido
            };
            request.Content = JsonContent.Create(cuerpo);

            using var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
                return new ResultadoEnvioEmail(true, null);

            var respuestaTexto = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Brevo respondió {StatusCode} al enviar correo a {Email}: {Respuesta}",
                (int)response.StatusCode, destinatarioEmail, respuestaTexto);
            return new ResultadoEnvioEmail(false, $"Brevo respondió {(int)response.StatusCode}: {respuestaTexto}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando correo a {Email}", destinatarioEmail);
            return new ResultadoEnvioEmail(false, ex.Message);
        }
    }
}
