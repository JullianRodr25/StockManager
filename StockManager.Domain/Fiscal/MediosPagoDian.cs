namespace StockManager.Domain.Fiscal;

/// <summary>
/// Traduce los métodos de pago del sistema a los códigos del catálogo de medios de pago de
/// la DIAN que piden los proveedores tecnológicos. Vive en el dominio (no en un proveedor)
/// porque es el mismo para todos.
/// </summary>
public static class MediosPagoDian
{
    /// <summary>Código DIAN del método de pago. "Mixto" no es un medio: se factura por cada parte.</summary>
    public static string CodigoDe(string metodoPago) => metodoPago switch
    {
        "Efectivo" => "10",
        "Tarjeta" => "48",       // Tarjeta crédito (la DIAN distingue 48 crédito y 49 débito; el sistema no lo distingue)
        "Transferencia" => "42", // Consignación bancaria / transferencia
        _ => throw new ArgumentException($"El método de pago '{metodoPago}' no tiene un único código DIAN (para Mixto use el de cada parte).", nameof(metodoPago))
    };
}
