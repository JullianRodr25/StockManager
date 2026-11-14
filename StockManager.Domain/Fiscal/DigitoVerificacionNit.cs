namespace StockManager.Domain.Fiscal;

/// <summary>
/// Dígito de verificación (DV) del NIT colombiano, con el algoritmo módulo 11 de la DIAN.
/// El DV se CALCULA, no se guarda: es una función pura del número, así que almacenarlo solo
/// abriría la puerta a que quede desincronizado.
/// </summary>
public static class DigitoVerificacionNit
{
    // Pesos de la DIAN, aplicados de derecha a izquierda sobre los dígitos del NIT.
    private static readonly int[] Pesos = { 3, 7, 13, 17, 19, 23, 29, 37, 41, 43, 47, 53, 59, 67, 71 };

    /// <summary>Calcula el DV de un NIT sin DV (solo dígitos, hasta 15).</summary>
    public static int Calcular(string nitSinDv)
    {
        if (string.IsNullOrWhiteSpace(nitSinDv) || nitSinDv.Length > Pesos.Length || !nitSinDv.All(char.IsAsciiDigit))
            throw new ArgumentException("El NIT debe tener solo dígitos (máximo 15) para calcular el dígito de verificación.", nameof(nitSinDv));

        var suma = 0;
        for (var i = 0; i < nitSinDv.Length; i++)
            suma += (nitSinDv[nitSinDv.Length - 1 - i] - '0') * Pesos[i];

        var residuo = suma % 11;
        return residuo > 1 ? 11 - residuo : residuo;
    }
}
