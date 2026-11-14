using System.Text;
using System.Text.RegularExpressions;

namespace StockManager.Domain.Common;

/// <summary>
/// Normaliza la capitalización de textos que se IMPRIMEN (productos, nombres, direcciones) sin
/// tocar lo guardado: "GALON BLANCA TIPO 1" y "galon blanca tipo 1" salen igual, "Galon Blanca
/// Tipo 1". Es solo presentación: la base de datos conserva lo que escribió el usuario.
/// </summary>
public static partial class FormatoTexto
{
    // Conectores que van en minúscula salvo al inicio ("Tubo de Cobre" → "Tubo de Cobre").
    private static readonly HashSet<string> Conectores = new(StringComparer.OrdinalIgnoreCase)
    {
        "de", "del", "la", "las", "el", "los", "y", "e", "o", "u", "a", "en", "con", "sin", "para", "por", "al",
    };

    // Siglas y abreviaturas que deben conservar su forma habitual.
    private static readonly Dictionary<string, string> Siglas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pvc"] = "PVC", ["cpvc"] = "CPVC", ["upvc"] = "UPVC", ["led"] = "LED", ["abs"] = "ABS",
        ["hdpe"] = "HDPE", ["nit"] = "NIT", ["sas"] = "SAS", ["sa"] = "SA", ["ltda"] = "Ltda", ["usb"] = "USB",
        ["gl"] = "GL", ["mm"] = "mm", ["cm"] = "cm", ["kg"] = "kg", ["gr"] = "gr", ["ml"] = "ml", ["lt"] = "lt",
        ["lb"] = "lb", ["mts"] = "mts", ["pulg"] = "pulg", ["ea"] = "EA",
    };

    [GeneratedRegex(@"\p{L}+", RegexOptions.CultureInvariant)]
    private static partial Regex Palabras();

    /// <summary>"GALON BLANCA tipo 1" → "Galon Blanca Tipo 1". Null/blanco se devuelven tal cual.</summary>
    public static string? ATitulo(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return texto;

        var entrada = texto.Trim();
        var resultado = new StringBuilder(entrada.Length);
        var ultimo = 0;

        foreach (Match m in Palabras().Matches(entrada))
        {
            resultado.Append(entrada, ultimo, m.Index - ultimo);
            ultimo = m.Index + m.Length;

            var palabra = m.Value;
            var esInicio = m.Index == 0;
            var pegadaANumero = m.Index > 0 && char.IsDigit(entrada[m.Index - 1]);

            var esInicial = palabra.Length == 1 && m.Index + 1 < entrada.Length && entrada[m.Index + 1] == '.';

            if (esInicial)
                // "S.A.S.", "J. Pérez": una letra seguida de punto es inicial o sigla.
                resultado.Append(char.ToUpperInvariant(palabra[0]));
            else if (Siglas.TryGetValue(palabra, out var sigla))
                resultado.Append(sigla);
            else if (pegadaANumero)
                // "10MM", "12V", "3M": lo que va pegado a un número es unidad o marca, no una palabra.
                resultado.Append(palabra.Length <= 2 ? palabra : palabra.ToLowerInvariant());
            else if (!esInicio && Conectores.Contains(palabra))
                resultado.Append(palabra.ToLowerInvariant());
            else
                resultado.Append(char.ToUpperInvariant(palabra[0])).Append(palabra[1..].ToLowerInvariant());
        }

        resultado.Append(entrada, ultimo, entrada.Length - ultimo);
        return resultado.ToString();
    }
}
