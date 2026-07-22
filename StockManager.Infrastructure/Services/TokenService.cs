using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StockManager.Application.Services;

namespace StockManager.Infrastructure.Services;

/// <summary>
/// Implementación de servicio de generación de tokens JWT.
/// </summary>
public class TokenService : ITokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public TokenService(IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        _secretKey = jwtSettings["SecretKey"] 
            ?? throw new InvalidOperationException("JWT SecretKey no configurada en appsettings.json");
        _issuer = jwtSettings["Issuer"] 
            ?? throw new InvalidOperationException("JWT Issuer no configurada en appsettings.json");
        _audience = jwtSettings["Audience"] 
            ?? throw new InvalidOperationException("JWT Audience no configurada en appsettings.json");

        if (!int.TryParse(jwtSettings["ExpirationMinutes"], out _expirationMinutes))
            _expirationMinutes = 60;  // Default 60 minutos
    }

    public string GenerarTokenEmpleado(int empleadoId, string numeroIdentificacion, string nombre, string rol)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, empleadoId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, numeroIdentificacion),
            new(JwtRegisteredClaimNames.GivenName, nombre),
            new("TipoUsuario", "Empleado"),
            new(ClaimTypes.Role, rol)
        };

        return GenerarToken(claims);
    }

    public string GenerarTokenCliente(int clienteId, string numeroIdentificacion, string nombre)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, clienteId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, numeroIdentificacion),
            new(JwtRegisteredClaimNames.GivenName, nombre),
            new("TipoUsuario", "Cliente")
        };

        return GenerarToken(claims);
    }

    private string GenerarToken(List<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: credentials
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }
}
