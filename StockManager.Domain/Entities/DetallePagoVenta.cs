namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad DetallePagoVenta del dominio.
/// Representa una línea del desglose de pago cuando Venta.MetodoPago es "Mixto": cuánto de
/// esa venta se pagó con cada método individual. Solo existen filas de esta tabla para ventas
/// Mixtas — con un método único no hay nada que desglosar, así que no se crea ninguna fila
/// (ver Venta.ValidarDetallesPago).
///
/// Mismo patrón que AbonoCuenta (uno-a-muchos sobre Venta, FK Restrict para no perder el
/// historial), pero estas líneas se registran todas juntas al momento de pagar la venta, no
/// secuencialmente en el tiempo como los abonos de una cuenta fiada.
/// </summary>
public class DetallePagoVenta
{
    private static readonly string[] MetodosPagoValidos = { "Efectivo", "Tarjeta", "Transferencia" };

    public int Id { get; private set; }
    public int VentaId { get; private set; }
    public string MetodoPago { get; private set; } = null!;
    public decimal Monto { get; private set; }

    private DetallePagoVenta() { }

    public static DetallePagoVenta Crear(int ventaId, string metodoPago, decimal monto)
    {
        if (ventaId <= 0)
            throw new ArgumentException("VentaId debe ser mayor a 0.", nameof(ventaId));

        if (!MetodosPagoValidos.Contains(metodoPago))
            throw new ArgumentException($"El método de pago '{metodoPago}' no es válido para una línea de detalle de pago.", nameof(metodoPago));

        if (monto <= 0)
            throw new ArgumentException("El monto debe ser mayor a 0.", nameof(monto));

        return new DetallePagoVenta
        {
            VentaId = ventaId,
            MetodoPago = metodoPago,
            Monto = monto
        };
    }
}
