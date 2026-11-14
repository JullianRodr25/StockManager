namespace StockManager.Domain.Fiscal;

/// <summary>
/// Reglas de los datos fiscales que exige la DIAN para identificar a quien se factura:
/// tipo de documento, tipo de persona y municipio. Centraliza la validación para que Cliente
/// y Venta (que guarda una copia al facturar) apliquen exactamente las mismas.
/// </summary>
public static class DocumentoFiscal
{
    public const string Natural = "Natural";
    public const string Juridica = "Juridica";
    public static readonly string[] TiposPersona = { Natural, Juridica };
    public static readonly string[] TiposDocumento = { "CC", "NIT", "CE", "Pasaporte", "Otro" };

    /// <summary>Valida el tipo de persona ("Natural" o "Juridica"); null/vacío significa sin dato.</summary>
    public static string? NormalizarTipoPersona(string? tipoPersona)
    {
        if (string.IsNullOrWhiteSpace(tipoPersona))
            return null;

        var valor = tipoPersona.Trim();
        if (!TiposPersona.Contains(valor))
            throw new ArgumentException($"El tipo de persona '{tipoPersona}' no es válido (use Natural o Juridica).", nameof(tipoPersona));
        return valor;
    }

    /// <summary>
    /// Valida el código DANE del municipio (5 dígitos, ej. 11001 = Bogotá). Solo se valida el
    /// formato —no una lista cerrada— para no rechazar un municipio legítimo si un catálogo
    /// local estuviera incompleto.
    /// </summary>
    public static string? NormalizarCodigoMunicipio(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return null;

        var valor = codigo.Trim();
        if (valor.Length != 5 || !valor.All(char.IsAsciiDigit))
            throw new ArgumentException("El código del municipio debe tener 5 dígitos (código DANE, ej. 11001).", nameof(codigo));
        return valor;
    }

    /// <summary>
    /// Normaliza el número de documento según su tipo. Para NIT acepta "900.123.456-8":
    /// quita los separadores, comprueba que el dígito de verificación (si lo escribieron)
    /// coincida con el calculado, y devuelve el NIT SIN dígito (así se guarda y así lo piden
    /// los proveedores, que reciben el DV aparte). Para CC deja solo dígitos. Para el resto
    /// solo recorta espacios.
    /// </summary>
    public static string NormalizarNumero(string tipo, string numero)
    {
        var texto = numero.Trim();

        switch (tipo)
        {
            case "NIT":
            {
                var partes = texto.Replace(".", "").Replace(" ", "").Split('-');
                if (partes.Length > 2)
                    throw new ArgumentException("El NIT solo puede llevar un guion antes del dígito de verificación.");

                var nit = partes[0];
                if (nit.Length is < 5 or > 15 || !nit.All(char.IsAsciiDigit))
                    throw new ArgumentException("El NIT debe tener entre 5 y 15 dígitos (sin dígito de verificación).");

                if (partes.Length == 2)
                {
                    var esperado = DigitoVerificacionNit.Calcular(nit);
                    if (partes[1] != esperado.ToString())
                        throw new ArgumentException($"El dígito de verificación del NIT no coincide: para {nit} debería ser {esperado}.");
                }
                return nit;
            }
            case "CC":
            {
                var cedula = texto.Replace(".", "").Replace(" ", "").Replace(",", "");
                if (cedula.Length is < 4 or > 12 || !cedula.All(char.IsAsciiDigit))
                    throw new ArgumentException("La cédula debe tener solo dígitos (entre 4 y 12).");
                return cedula;
            }
            default:
                return texto;
        }
    }

    /// <summary>
    /// Una persona jurídica (empresa) se identifica siempre con NIT; una natural puede usar
    /// cualquier documento (incluso NIT, si es comerciante). Evita combinaciones como
    /// "Jurídica con cédula", que la DIAN rechazaría.
    /// </summary>
    public static void ValidarCoherencia(string tipoDocumento, string? tipoPersona)
    {
        if (tipoPersona == Juridica && tipoDocumento != "NIT")
            throw new ArgumentException("Una persona jurídica debe identificarse con NIT.");
    }
}
