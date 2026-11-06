namespace StockManager.Domain.Entities;

/// <summary>
/// Una línea de una <see cref="Devolucion"/>: o algo que el cliente devuelve, o algo que se lleva
/// a cambio. Comparten tabla porque es el mismo hecho visto desde dos lados (producto, cantidad y
/// precio); <see cref="Sentido"/> los distingue.
/// </summary>
public class DetalleDevolucion
{
    public const string SentidoDevuelto = "Devuelto";
    public const string SentidoEntregado = "Entregado";

    public int Id { get; private set; }
    public int DevolucionId { get; private set; }

    /// <summary>"Devuelto" (vuelve del cliente) o "Entregado" (sale hacia el cliente a cambio).</summary>
    public string Sentido { get; private set; } = null!;

    /// <summary>Línea de la venta original que se devuelve. Null en las líneas entregadas a cambio.</summary>
    public int? DetalleVentaId { get; private set; }

    public int ProductoId { get; private set; }
    public int Cantidad { get; private set; }

    /// <summary>Devuelto: precio de la venta original. Entregado: precio vigente al momento del cambio.</summary>
    public decimal PrecioUnitario { get; private set; }

    /// <summary>
    /// Solo aplica a lo devuelto: false cuando el producto no vuelve a la estantería (dañado,
    /// usado). Esa mercancía no se vuelve a sumar al stock.
    /// </summary>
    public bool ReintegradoAlStock { get; private set; }

    public decimal Subtotal => Cantidad * PrecioUnitario;

    private DetalleDevolucion() { }

    public static DetalleDevolucion CrearDevuelto(
        int devolucionId, int detalleVentaId, int productoId, int cantidad, decimal precioUnitario, bool reintegrarAlStock)
    {
        if (detalleVentaId <= 0)
            throw new ArgumentException("DetalleVentaId debe ser mayor a 0.", nameof(detalleVentaId));

        return Crear(devolucionId, SentidoDevuelto, detalleVentaId, productoId, cantidad, precioUnitario, reintegrarAlStock);
    }

    public static DetalleDevolucion CrearEntregado(
        int devolucionId, int productoId, int cantidad, decimal precioUnitario)
    {
        return Crear(devolucionId, SentidoEntregado, null, productoId, cantidad, precioUnitario, false);
    }

    private static DetalleDevolucion Crear(
        int devolucionId, string sentido, int? detalleVentaId, int productoId, int cantidad, decimal precioUnitario, bool reintegrado)
    {
        if (devolucionId <= 0)
            throw new ArgumentException("DevolucionId debe ser mayor a 0.", nameof(devolucionId));

        if (productoId <= 0)
            throw new ArgumentException("ProductoId debe ser mayor a 0.", nameof(productoId));

        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a 0.", nameof(cantidad));

        if (precioUnitario < 0)
            throw new ArgumentException("El precio no puede ser negativo.", nameof(precioUnitario));

        return new DetalleDevolucion
        {
            DevolucionId = devolucionId,
            Sentido = sentido,
            DetalleVentaId = detalleVentaId,
            ProductoId = productoId,
            Cantidad = cantidad,
            PrecioUnitario = precioUnitario,
            ReintegradoAlStock = reintegrado
        };
    }
}
