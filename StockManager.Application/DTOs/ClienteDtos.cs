using System.ComponentModel.DataAnnotations;

namespace StockManager.Application.DTOs;

public record ClienteResponse(
    int Id,
    string NumeroIdentificacion,
    string Nombre,
    /// <summary>Opcional para clientes de caja; obligatorio en los de la PWA.</summary>
    string? Email,
    string Telefono,
    string Direccion,
    /// <summary>Coordenadas del pin guardado desde "Mi cuenta" (ver Cliente.Latitud/Longitud). Null si nunca se fijó.</summary>
    double? Latitud,
    double? Longitud,
    bool Activo,
    /// <summary>"Pwa" o "Caja". Ver Cliente.OrigenRegistro.</summary>
    string OrigenRegistro,
    string? TipoDocumentoFiscal,
    string? NumeroDocumentoFiscal,
    string? RazonSocialFiscal,
    string? DireccionFiscal,
    string? EmailFacturacion,
    bool TieneDatosFacturacionElectronicaCompletos,
    string? FotoUrl
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
    string? Email,
    string Telefono,
    string Direccion,
    string? Password,
    string? TipoDocumentoFiscal = null,
    string? NumeroDocumentoFiscal = null,
    string? RazonSocialFiscal = null,
    string? DireccionFiscal = null,
    string? EmailFacturacion = null
);

/// <summary>
/// Latitud/Longitud son opcionales (selector de mapa en "Mi cuenta" de la PWA, ver
/// MapaDireccion): si no se envían (ej. el panel admin, que edita solo texto), ClienteService
/// conserva las coordenadas que el cliente ya tuviera guardadas en vez de borrarlas.
/// </summary>
public record ActualizarClienteRequest(
    string Nombre,
    string? Email,
    string Telefono,
    string Direccion,
    double? Latitud = null,
    double? Longitud = null
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

/// <summary>
/// DTO para que el propio cliente autenticado cambie su contraseña desde "Mi cuenta" en la
/// PWA (distinto del flujo de recuperación por correo, que no requiere conocer la actual).
/// Exige PasswordActual para evitar que una sesión robada/dejada abierta pueda tomar control
/// total de la cuenta con solo cambiar la contraseña.
/// </summary>
public record CambiarPasswordPropioRequest(
    [Required(ErrorMessage = "La contraseña actual es requerida")]
    string PasswordActual,

    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [MinLength(8, ErrorMessage = "La nueva contraseña debe tener al menos 8 caracteres")]
    string PasswordNueva
);

/// <summary>
/// Resultado de importar el Excel de clientes (o de su vista previa). Los "avisos" no frenan la
/// importación (cliente ya existente, otra categoría, correo repetido, dato faltante); los
/// "errores" sí: con uno solo no se guarda nada (todo o nada).
/// </summary>
public class ImportarClientesResponse
{
    public int TotalFilas { get; set; }
    public int Creados { get; set; }

    /// <summary>Cédulas/NIT que ya existían en el sistema: se dejan como están.</summary>
    public int YaExistentes { get; set; }

    /// <summary>Terceros de otra categoría (nómina, contabilidad...) que no son clientes.</summary>
    public int OtraCategoria { get; set; }

    /// <summary>True solo si los clientes se guardaron (false en vista previa o con errores).</summary>
    public bool Aplicado { get; set; }
    public List<ErrorImportacion> Avisos { get; set; } = new();
    public List<ErrorImportacion> Errores { get; set; } = new();
}
