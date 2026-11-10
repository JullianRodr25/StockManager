namespace StockManager.Domain.Entities;

/// <summary>
/// Línea de una cotización. Guarda el precio unitario OFRECIDO: es el precio que se respeta al
/// convertir la cotización en venta mientras esté vigente, aunque el precio de lista cambie.
/// </summary>
public class DetalleCotizacion
{
    public int Id { get; private set; }
    public int CotizacionId { get; private set; }
    public int ProductoId { get; private set; }
    public int Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }

    public decimal Subtotal => Cantidad * PrecioUnitario;

    private DetalleCotizacion() { }

    internal static DetalleCotizacion Crear(int productoId, int cantidad, decimal precioUnitario)
    {
        if (productoId <= 0)
            throw new ArgumentException("ProductoId debe ser mayor a 0.", nameof(productoId));
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a 0.", nameof(cantidad));
        if (precioUnitario <= 0)
            throw new ArgumentException("El precio unitario debe ser mayor a 0.", nameof(precioUnitario));

        return new DetalleCotizacion
        {
            ProductoId = productoId,
            Cantidad = cantidad,
            PrecioUnitario = precioUnitario
        };
    }
}
