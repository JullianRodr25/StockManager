namespace StockManager.Application.Services;

/// <summary>
/// Interfaz para servicio de generación de tokens JWT.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Genera un token JWT para un empleado.
    /// </summary>
    string GenerarTokenEmpleado(int empleadoId, string numeroIdentificacion, string nombre, string rol);

    /// <summary>
    /// Genera un token JWT para un cliente.
    /// </summary>
    string GenerarTokenCliente(int clienteId, string numeroIdentificacion, string nombre);
}
