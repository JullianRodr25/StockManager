namespace StockManager.Application.DTOs;

public record ClienteResponse(
    int Id,
    string NumeroIdentificacion,
    string Nombre,
    string Email,
    string Telefono,
    string Direccion,
    bool Activo
);

/// <summary>
/// DTO para que un Empleado/Admin cree un cliente directamente desde el panel (a diferencia
/// del autoregistro público en la PWA). Password es opcional: si se deja vacío, el sistema
/// genera una contraseña temporal aleatoria y la devuelve una única vez en la respuesta para
/// que el empleado se la entregue al cliente (típico caso: un cliente de mostrador sin PWA
/// que solo necesita existir como Cliente para poder abrirle una Cuenta Abierta/fiado).
/// </summary>
public record CrearClienteRequest(
    string NumeroIdentificacion,
    string Nombre,
    string Email,
    string Telefono,
    string Direccion,
    string? Password
);

public record ActualizarClienteRequest(
    string Nombre,
    string Email,
    string Telefono,
    string Direccion
);

/// <summary>
/// Respuesta de creación de cliente. PasswordTemporal solo viene informado cuando el
/// Password no se especificó en el request (se generó una nueva); nunca se puede volver a
/// consultar después, ya que solo se guarda su hash.
/// </summary>
public record ClienteCreadoResponse(
    ClienteResponse Cliente,
    string? PasswordTemporal
);
