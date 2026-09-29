using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using StockManager.Application.Services;

namespace StockManager.Infrastructure.Notificaciones;

/// <summary>
/// Genera y valida el token firmado del endpoint público GET /api/facturas/{id}/pdf.
///
/// Formato del token: "{facturaId}.{expiraUnixSeconds}.{firmaBase64Url}", donde la firma es
/// un HMAC-SHA256 sobre "{facturaId}.{expiraUnixSeconds}" con la clave WhatsAppOptions.LinkSecret.
/// No hay estado en base de datos: el propio token contiene todo lo necesario para
/// validarlo, y expira solo por tiempo (no hay forma de revocarlo antes, pero la vigencia
/// es corta y el token solo se genera internamente al despachar una notificación).
/// </summary>
public class HmacFacturaLinkTokenService : IFacturaLinkTokenService
{
    private readonly IOptions<WhatsAppOptions> _opciones;

    public HmacFacturaLinkTokenService(IOptions<WhatsAppOptions> opciones)
    {
        _opciones = opciones;
    }

    public string GenerarToken(int facturaId, TimeSpan vigencia)
    {
        var expira = DateTimeOffset.UtcNow.Add(vigencia).ToUnixTimeSeconds();
        var payload = $"{facturaId}.{expira}";
        var firma = Firmar(payload);
        return $"{payload}.{firma}";
    }

    public bool ValidarToken(int facturaId, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var partes = token.Split('.');
        if (partes.Length != 3)
            return false;

        if (!int.TryParse(partes[0], out var facturaIdToken) || facturaIdToken != facturaId)
            return false;

        if (!long.TryParse(partes[1], out var expiraUnix))
            return false;

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiraUnix)
            return false; // Vencido

        var payload = $"{partes[0]}.{partes[1]}";
        var firmaEsperada = Firmar(payload);

        // Comparación en tiempo constante para no filtrar por timing cuánto de la firma coincide.
        var bytesEsperados = Encoding.UTF8.GetBytes(firmaEsperada);
        var bytesRecibidos = Encoding.UTF8.GetBytes(partes[2]);

        return bytesEsperados.Length == bytesRecibidos.Length &&
               CryptographicOperations.FixedTimeEquals(bytesEsperados, bytesRecibidos);
    }

    private string Firmar(string payload)
    {
        var claveSecreta = Encoding.UTF8.GetBytes(_opciones.Value.LinkSecret);
        using var hmac = new HMACSHA256(claveSecreta);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] datos) =>
        Convert.ToBase64String(datos).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
