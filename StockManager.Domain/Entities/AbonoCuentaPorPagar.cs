namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad AbonoCuentaPorPagar del dominio.
/// Representa un pago parcial hecho contra una CuentaPorPagar (dinero que le pagamos a un
/// proveedor). Es la contraparte de AbonoCuenta, que registra pagos que nos hacen a
/// nosotros; aquí es al revés, nosotros somos quienes pagamos.
/// </summary>
public class AbonoCuentaPorPagar
{
    private static readonly string[] MetodosPagoValidos = { "Efectivo", "Tarjeta", "Transferencia" };

    public int Id { get; private set; }
    public int CuentaPorPagarId { get; private set; }
    public decimal Monto { get; private set; }
    public string MetodoPago { get; private set; } = null!;
    public DateTime Fecha { get; private set; }
    public int EmpleadoId { get; private set; }

    private AbonoCuentaPorPagar() { }

    public static AbonoCuentaPorPagar Crear(int cuentaPorPagarId, decimal monto, string metodoPago, int empleadoId)
    {
        if (cuentaPorPagarId <= 0)
            throw new ArgumentException("CuentaPorPagarId debe ser mayor a 0.", nameof(cuentaPorPagarId));

        if (monto <= 0)
            throw new ArgumentException("El monto debe ser mayor a 0.", nameof(monto));

        if (!MetodosPagoValidos.Contains(metodoPago))
            throw new ArgumentException($"El método de pago '{metodoPago}' no es válido.", nameof(metodoPago));

        if (empleadoId <= 0)
            throw new ArgumentException("EmpleadoId debe ser mayor a 0.", nameof(empleadoId));

        return new AbonoCuentaPorPagar
        {
            CuentaPorPagarId = cuentaPorPagarId,
            Monto = monto,
            MetodoPago = metodoPago,
            Fecha = DateTime.UtcNow,
            EmpleadoId = empleadoId
        };
    }
}
