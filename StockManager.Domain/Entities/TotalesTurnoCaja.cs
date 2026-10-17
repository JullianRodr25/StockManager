namespace StockManager.Domain.Entities;

/// <summary>
/// Totales de dinero que pasaron por un turno de caja, separados por método de pago.
///
/// Es un valor puro (sin dependencias de EF ni de infraestructura) a propósito: la fórmula del
/// efectivo esperado es la regla más delicada del módulo —es dinero— y así se puede probar sin
/// base de datos. Solo el efectivo entra en la fórmula; transferencias y tarjeta se guardan
/// únicamente para conciliar contra el banco y el datáfono.
/// </summary>
public sealed record TotalesTurnoCaja(
    decimal VentasEfectivo,
    decimal AbonosFiadoEfectivo,
    decimal PagosProveedorEfectivo,
    decimal Transferencias,
    decimal Tarjeta)
{
    public static readonly TotalesTurnoCaja Vacio = new(0m, 0m, 0m, 0m, 0m);

    /// <summary>
    /// Efectivo que debería haber en el cajón: base inicial + lo que entró en efectivo − lo que
    /// salió en efectivo. Las ventas cuentan por su total (no por lo recibido), porque el cambio
    /// devuelto al cliente nunca se queda en la caja.
    /// </summary>
    public decimal EfectivoEsperado(decimal baseInicial) =>
        baseInicial + VentasEfectivo + AbonosFiadoEfectivo - PagosProveedorEfectivo;
}
