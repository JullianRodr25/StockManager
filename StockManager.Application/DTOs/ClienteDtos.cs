namespace StockManager.Application.DTOs;

public record ClienteResponse(
    int Id,
    string NumeroIdentificacion,
    string Nombre,
    string Email,
    string Telefono,
    string Direccion,
    bool Activo,
    /// <summary>"Pwa" o "Caja". Ver Cliente.OrigenRegistro.</summary>
    string OrigenRegistro,
    string? TipoDocumentoFiscal,
    string? NumeroDocumentoFiscal,
    string? RazonSocialFiscal,
    string? DireccionFiscal,
    string? EmailFacturacion,
    bool TieneDatosFacturacionElectronicaCompletos
);

/// <summary>
/// DTO para que un Empleado/Admin cree un cliente directamente desde el panel (a diferencia
/// del autoregistro público en la PWA). Password es opcional: si se deja vacío, el sistema
/// genera una contraseña temporal aleatoria y la devuelve una única vez en la respuesta para
/// que el empleado se la entregue al cliente (típico caso: un cliente de mostrador sin PWA
/// que solo necesita existir como Cliente para poder abrirle una Cuenta Abierta/fiado).
/// Los datos fiscales son opcionales: permiten el registro rápido de un cliente que, en el
/// mismo momento de la venta, pide que le hagan factura electrónica (ver RegistrarVentaRequest).
/// </summary>
public record CrearClienteRequest(
    string NumeroIdentificacion,
    string Nombre,
    string Email,
    string Telefono,
    string Direccion,
    string? Password,
    string? TipoDocumentoFiscal = null,
    string? NumeroDocumentoFiscal = null,
    string? RazonSocialFiscal = null,
    string? DireccionFiscal = null,
    string? EmailFacturacion = null
);

public record ActualizarClienteRequest(
    string Nombre,
    string Email,
    string Telefono,
    string Direccion
);

/// <summary>
/// Actualiza los datos fiscales de un cliente ya existente (sección "Datos para factura
/// electrónica" del formulario, separada de los datos de contacto). Cualquier campo que
/// llegue null/vacío queda limpio.
/// </summary>
public record ActualizarDatosFacturacionRequest(
    string? TipoDocumentoFiscal,
    string? NumeroDocumentoFiscal,
    string? RazonSocialFiscal,
    string? DireccionFiscal,
    string? EmailFacturacion
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
