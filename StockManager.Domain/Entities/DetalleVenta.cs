namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad DetalleVenta del dominio.
/// Representa una línea de detalle dentro de una venta.
/// </summary>
public class DetalleVenta
{
    public int Id { get; private set; }
    public int VentaId { get; private set; }
    public int ProductoId { get; private set; }
    public int Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }

    private DetalleVenta() { }

    public static DetalleVenta Crear(int ventaId, int productoId, int cantidad, decimal precioUnitario)
    {
        if (ventaId <= 0)
            throw new ArgumentException("VentaId debe ser mayor a 0.", nameof(ventaId));

        if (productoId <= 0)
            throw new ArgumentException("ProductoId debe ser mayor a 0.", nameof(productoId));

        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a 0.", nameof(cantidad));

        if (precioUnitario <= 0)
            throw new ArgumentException("El precio unitario debe ser mayor a 0.", nameof(precioUnitario));

        return new DetalleVenta
        {
            VentaId = ventaId,
            ProductoId = productoId,
            Cantidad = cantidad,
            PrecioUnitario = precioUnitario
        };
    }

    /// <summary>
    /// Actualiza la cantidad de la línea. El precio unitario no cambia.
    /// </summary>
    public void ActualizarCantidad(int nuevaCantidad)
    {
        if (nuevaCantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a 0.", nameof(nuevaCantidad));

        Cantidad = nuevaCantidad;
    }
}
