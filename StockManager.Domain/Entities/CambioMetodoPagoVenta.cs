namespace StockManager.Domain.Entities;

/// <summary>
/// Registro de auditoría de un cambio de método de pago sobre una venta ya pagada. Corregir el
/// método es legítimo (error de digitación en caja), pero mueve dinero entre columnas del
/// cierre de caja, así que cada cambio deja huella: quién, cuándo y de qué a qué.
/// Es historial inmutable: solo se crea, nunca se edita.
/// </summary>
public class CambioMetodoPagoVenta
{
    public int Id { get; private set; }
    public int VentaId { get; private set; }
    public int EmpleadoId { get; private set; }
    public DateTime Fecha { get; private set; }
    public string MetodoAnterior { get; private set; } = null!;
    public string MetodoNuevo { get; private set; } = null!;

    private CambioMetodoPagoVenta() { }

    public static CambioMetodoPagoVenta Crear(int ventaId, int empleadoId, string metodoAnterior, string metodoNuevo)
    {
        if (ventaId <= 0)
            throw new ArgumentException("VentaId debe ser mayor a 0.", nameof(ventaId));
        if (empleadoId <= 0)
            throw new ArgumentException("EmpleadoId debe ser mayor a 0.", nameof(empleadoId));

        return new CambioMetodoPagoVenta
        {
            VentaId = ventaId,
            EmpleadoId = empleadoId,
            Fecha = DateTime.UtcNow,
            MetodoAnterior = metodoAnterior,
            MetodoNuevo = metodoNuevo
        };
    }
}
