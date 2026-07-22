namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad Venta del dominio.
/// Representa una venta realizada en el mostrador o una cotización.
/// </summary>
public class Venta
{
    public int Id { get; private set; }
    public int EmpleadoId { get; private set; }
    public int? ClienteId { get; private set; }
    public DateTime Fecha { get; private set; }
    public decimal Total { get; private set; }
    public bool EsCotizacion { get; private set; }

    private Venta() { }

    public static Venta Crear(int empleadoId, int? clienteId, decimal total, bool esCotizacion)
    {
        if (empleadoId <= 0)
            throw new ArgumentException("EmpleadoId debe ser mayor a 0.", nameof(empleadoId));

        if (total < 0)
            throw new ArgumentException("El total no puede ser negativo.", nameof(total));

        return new Venta
        {
            EmpleadoId = empleadoId,
            ClienteId = clienteId,
            Fecha = DateTime.UtcNow,
            Total = total,
            EsCotizacion = esCotizacion
        };
    }
}
