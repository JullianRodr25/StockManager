using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockManager.Application.Services;

namespace StockManager.Infrastructure.Notificaciones;

/// <summary>
/// Implementación de IWhatsAppSender sobre la API REST de Twilio (sin SDK: la superficie
/// que se usa es mínima —un POST form-encoded con autenticación básica— y evitar el paquete
/// oficial evita una dependencia pesada para dos endpoints). Se registra con
/// AddHttpClient&lt;IWhatsAppSender, TwilioWhatsAppSender&gt;() para reutilizar el
/// HttpClient (pooling de conexiones) y respetar los timeouts/handlers que configure Program.cs.
///
/// Nunca lanza excepción por un fallo del envío (credenciales, número inválido, Twilio caído,
/// red): todo eso se traduce a ResultadoEnvioWhatsApp(false, "..."), porque el llamador
/// (el BackgroundService) necesita seguir procesando el resto de la cola sin interrupciones.
/// </summary>
public class TwilioWhatsAppSender : IWhatsAppSender
{
    private readonly HttpClient _httpClient;
    private readonly TwilioOptions _opciones;
    private readonly ILogger<TwilioWhatsAppSender> _logger;

    public TwilioWhatsAppSender(HttpClient httpClient, IOptions<TwilioOptions> opciones, ILogger<TwilioWhatsAppSender> logger)
    {
        _httpClient = httpClient;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public Task<ResultadoEnvioWhatsApp> EnviarTextoAsync(string telefonoDestino, string mensaje) =>
        EnviarAsync(telefonoDestino, mensaje, urlDocumento: null);

    public Task<ResultadoEnvioWhatsApp> EnviarDocumentoAsync(string telefonoDestino, string mensaje, string urlDocumento) =>
        EnviarAsync(telefonoDestino, mensaje, urlDocumento);

    private async Task<ResultadoEnvioWhatsApp> EnviarAsync(string telefonoDestino, string mensaje, string? urlDocumento)
    {
        if (string.IsNullOrWhiteSpace(_opciones.AccountSid) || string.IsNullOrWhiteSpace(_opciones.AuthToken))
            return new ResultadoEnvioWhatsApp(false, "Credenciales de Twilio no configuradas.");

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"2010-04-01/Accounts/{_opciones.AccountSid}/Messages.json");

            var credenciales = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_opciones.AccountSid}:{_opciones.AuthToken}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credenciales);

            var campos = new List<KeyValuePair<string, string>>
            {
                new("To", ConPrefijoWhatsApp(telefonoDestino)),
                new("From", ConPrefijoWhatsApp(_opciones.FromWhatsAppNumber)),
                new("Body", mensaje)
            };

            if (!string.IsNullOrWhiteSpace(urlDocumento))
                campos.Add(new KeyValuePair<string, string>("MediaUrl", urlDocumento));

            request.Content = new FormUrlEncodedContent(campos);

            using var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
                return new ResultadoEnvioWhatsApp(true, null);

            var cuerpo = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Twilio respondió {StatusCode} al enviar WhatsApp a {Telefono}: {Cuerpo}",
                (int)response.StatusCode, telefonoDestino, cuerpo);
            return new ResultadoEnvioWhatsApp(false, $"Twilio respondió {(int)response.StatusCode}: {cuerpo}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando WhatsApp a {Telefono}", telefonoDestino);
            return new ResultadoEnvioWhatsApp(false, ex.Message);
        }
    }

    /// <summary>Twilio exige que To/From lleven el prefijo "whatsapp:" delante del número E.164.</summary>
    private static string ConPrefijoWhatsApp(string numero)
    {
        numero = numero.Trim();
        return numero.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase) ? numero : $"whatsapp:{numero}";
    }
}
