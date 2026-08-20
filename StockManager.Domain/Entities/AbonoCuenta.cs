namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad AbonoCuenta del dominio.
/// Representa un pago parcial hecho contra una cuenta fiada (Venta en estado Pendiente).
/// </summary>
public class AbonoCuenta
{
    private static readonly string[] MetodosPagoValidos = { "Efectivo", "Tarjeta", "Transferencia" };

    public int Id { get; private set; }
    public int VentaId { get; private set; }
    public decimal Monto { get; private set; }
    public string MetodoPago { get; private set; } = null!;
    public DateTime Fecha { get; private set; }
    public int EmpleadoId { get; private set; }

    private AbonoCuenta() { }

    public static AbonoCuenta Crear(int ventaId, decimal monto, string metodoPago, int empleadoId)
    {
        if (ventaId <= 0)
            throw new ArgumentException("VentaId debe ser mayor a 0.", nameof(ventaId));

        if (monto <= 0)
            throw new ArgumentException("El monto debe ser mayor a 0.", nameof(monto));

        if (!MetodosPagoValidos.Contains(metodoPago))
            throw new ArgumentException($"El método de pago '{metodoPago}' no es válido.", nameof(metodoPago));

        if (empleadoId <= 0)
            throw new ArgumentException("EmpleadoId debe ser mayor a 0.", nameof(empleadoId));

        return new AbonoCuenta
        {
            VentaId = ventaId,
            Monto = monto,
            MetodoPago = metodoPago,
            Fecha = DateTime.UtcNow,
            EmpleadoId = empleadoId
        };
    }
}
