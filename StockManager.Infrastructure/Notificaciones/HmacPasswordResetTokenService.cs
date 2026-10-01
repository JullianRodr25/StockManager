using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using StockManager.Application.Services;

namespace StockManager.Infrastructure.Notificaciones;

/// <summary>
/// Genera y valida el token firmado del flujo de recuperación de contraseña.
///
/// Formato del token: "{tipoUsuario}.{usuarioId}.{expiraUnixSeconds}.{firmaBase64Url}", donde
/// la firma es un HMAC-SHA256 sobre "{tipoUsuario}.{usuarioId}.{expiraUnixSeconds}" con la
/// clave EmailOptions.RecuperacionSecret. Mismo principio que HmacFacturaLinkTokenService:
/// sin estado en base de datos, expira solo por tiempo (no hay forma de revocarlo antes de
/// que expire ni de invalidarlo tras usarse una vez, pero la vigencia es corta y el token
/// solo autoriza cambiar la contraseña del propio usuario que lo recibió por correo).
/// </summary>
public class HmacPasswordResetTokenService : IPasswordResetTokenService
{
    private readonly IOptions<EmailOptions> _opciones;

    public HmacPasswordResetTokenService(IOptions<EmailOptions> opciones)
    {
        _opciones = opciones;
    }

    public string GenerarToken(string tipoUsuario, int usuarioId, TimeSpan vigencia)
    {
        var expira = DateTimeOffset.UtcNow.Add(vigencia).ToUnixTimeSeconds();
        var payload = $"{tipoUsuario}.{usuarioId}.{expira}";
        var firma = Firmar(payload);
        return $"{payload}.{firma}";
    }

    public PasswordResetTokenPayload? ValidarToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var partes = token.Split('.');
        if (partes.Length != 4)
            return null;

        var tipoUsuario = partes[0];
        if (tipoUsuario != "Empleado" && tipoUsuario != "Cliente")
            return null;

        if (!int.TryParse(partes[1], out var usuarioId))
            return null;

        if (!long.TryParse(partes[2], out var expiraUnix))
            return null;

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiraUnix)
            return null; // Vencido

        var payload = $"{partes[0]}.{partes[1]}.{partes[2]}";
        var firmaEsperada = Firmar(payload);

        // Comparación en tiempo constante para no filtrar por timing cuánto de la firma coincide.
        var bytesEsperados = Encoding.UTF8.GetBytes(firmaEsperada);
        var bytesRecibidos = Encoding.UTF8.GetBytes(partes[3]);

        if (bytesEsperados.Length != bytesRecibidos.Length ||
            !CryptographicOperations.FixedTimeEquals(bytesEsperados, bytesRecibidos))
            return null;

        return new PasswordResetTokenPayload(tipoUsuario, usuarioId);
    }

    private string Firmar(string payload)
    {
        var claveSecreta = Encoding.UTF8.GetBytes(_opciones.Value.RecuperacionSecret);
        using var hmac = new HMACSHA256(claveSecreta);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] datos) =>
        Convert.ToBase64String(datos).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
