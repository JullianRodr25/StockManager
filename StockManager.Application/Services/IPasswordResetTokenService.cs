namespace StockManager.Application.Services;

/// <summary>
/// Identifica de forma única, dentro del token de recuperación, a qué usuario pertenece
/// (Empleado o Cliente son tablas separadas, aunque el email sea globalmente único entre
/// ambas — ver AuthService.RegistrarClienteAsync).
/// </summary>
public record PasswordResetTokenPayload(string TipoUsuario, int UsuarioId);

/// <summary>
/// Genera y valida el token firmado que autoriza restablecer la contraseña de un usuario
/// puntual, enviado por correo (link "¿olvidaste tu contraseña?"). Mismo esquema que
/// IFacturaLinkTokenService: sin estado en base de datos, el propio token contiene todo lo
/// necesario para validarlo y expira solo por tiempo. A diferencia de aquel, acá el llamador
/// no conoce de antemano a quién pertenece el token (llega crudo desde el link que el
/// usuario abrió), así que ValidarToken debe devolver el payload en vez de solo confirmar
/// un id ya conocido.
/// </summary>
public interface IPasswordResetTokenService
{
    string GenerarToken(string tipoUsuario, int usuarioId, TimeSpan vigencia);

    /// <summary>Retorna el payload si el token es válido y no ha expirado, o null en caso contrario.</summary>
    PasswordResetTokenPayload? ValidarToken(string token);
}
