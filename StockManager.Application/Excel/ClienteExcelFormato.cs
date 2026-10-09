namespace StockManager.Application.Excel;

/// <summary>
/// Contrato del Excel de clientes que se importa: el que exporta el programa contable anterior
/// del negocio (terceros). Las columnas se buscan por el NOMBRE del encabezado, no por posición,
/// así que el orden no importa y las columnas que no usamos (régimen, responsable de IVA,
/// departamento...) se ignoran sin problema.
/// </summary>
public static class ClienteExcelFormato
{
    public const int FilaEncabezado = 1;
    public const int PrimeraFilaDatos = 2;

    public const string ColTipoPersona = "TIPO";
    public const string ColIdentificacion = "IDENTIFICACION";
    public const string ColDigitoVerificacion = "DIGITO_VERIFICACION";
    public const string ColTipoIdentificacion = "TIPO_IDENTIFICACION";
    public const string ColNombre = "NOMBRE";
    public const string ColOtrosNombres = "OTROS_NOMBRES";
    public const string ColApellido = "APELLIDO";
    public const string ColSegundoApellido = "SEGUNDO_APELLIDO";
    public const string ColRazonSocial = "RAZON_SOCIAL";
    public const string ColCorreo = "CORREO";
    public const string ColTelefono = "TELEFONO";
    public const string ColDireccion = "DIRECCION";
    public const string ColCategoria = "CATEGORIA";

    /// <summary>Sin estas columnas el archivo no es un Excel de terceros y se rechaza completo.</summary>
    public static readonly string[] EncabezadosObligatorios =
    {
        ColIdentificacion,
        ColTipoIdentificacion,
    };

    /// <summary>
    /// Solo se importan los terceros de esta categoría; el programa contable también guarda ahí
    /// entidades de nómina (SENA, ICBF...) y personas de contabilidad que no son clientes.
    /// </summary>
    public const string CategoriaClientes = "CLIENTE";

    /// <summary>
    /// Teléfono y dirección son obligatorios en el sistema, pero casi ningún tercero los trae.
    /// Se rellenan con este texto para no rechazar al cliente; se corrigen luego desde Clientes.
    /// </summary>
    public const string TextoSinDato = "Sin dato";
}
