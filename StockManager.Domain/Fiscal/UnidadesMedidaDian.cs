namespace StockManager.Domain.Fiscal;

/// <summary>
/// Unidades de medida más usadas en una ferretería, con el código del catálogo de la DIAN
/// (basado en UN/ECE Rec. 20). El predeterminado es "94" (unidad). Se valida contra esta
/// lista corta a propósito: sirve para que el producto salga con una unidad válida; si hiciera
/// falta otra, se agrega aquí.
/// </summary>
public static class UnidadesMedidaDian
{
    public const string Unidad = "94";

    public static readonly IReadOnlyDictionary<string, string> Catalogo = new Dictionary<string, string>
    {
        ["94"] = "Unidad",
        ["KGM"] = "Kilogramo",
        ["GRM"] = "Gramo",
        ["LBR"] = "Libra",
        ["MTR"] = "Metro",
        ["CMT"] = "Centímetro",
        ["MTK"] = "Metro cuadrado",
        ["MTQ"] = "Metro cúbico",
        ["LTR"] = "Litro",
        ["GLL"] = "Galón",
        ["BX"] = "Caja",
        ["BG"] = "Bolsa",
        ["DZN"] = "Docena",
        ["PR"] = "Par",
    };

    public static string Validar(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return Unidad;

        var valor = codigo.Trim().ToUpperInvariant();
        if (!Catalogo.ContainsKey(valor))
            throw new ArgumentException($"La unidad de medida '{codigo}' no es válida.", nameof(codigo));
        return valor;
    }
}
