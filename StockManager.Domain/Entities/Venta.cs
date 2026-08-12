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
    public string? NombreComprador { get; private set; }
    public string? TelefonoComprador { get; private set; }
    public string? EmailComprador { get; private set; }
    public string MetodoPago { get; private set; } = null!;
    public DateTime Fecha { get; private set; }
    public decimal Total { get; private set; }
    public bool EsCotizacion { get; private set; }
    public string Estado { get; private set; } = null!;

    private Venta() { }

    public static Venta Crear(
        int empleadoId,
        int? clienteId,
        string? nombreComprador,
        string? telefonoComprador,
        string? emailComprador,
        string metodoPago,
        decimal total,
        bool esCotizacion,
        string estado)
    {
        if (empleadoId <= 0)
            throw new ArgumentException("EmpleadoId debe ser mayor a 0.", nameof(empleadoId));

        if (total < 0)
            throw new ArgumentException("El total no puede ser negativo.", nameof(total));

        var estadosValidos = new[] { "Pendiente", "Pagada", "Cancelada" };
        if (!estadosValidos.Contains(estado))
            throw new ArgumentException($"El estado '{estado}' no es válido.", nameof(estado));

        var metodosPagoValidos = new[] { "Efectivo", "Tarjeta", "Transferencia" };
        if (!metodosPagoValidos.Contains(metodoPago))
            throw new ArgumentException($"El método de pago '{metodoPago}' no es válido.", nameof(metodoPago));

        if (clienteId is null && string.IsNullOrWhiteSpace(nombreComprador))
            throw new ArgumentException("Debe indicar un cliente registrado o el nombre del comprador.");

        return new Venta
        {
            EmpleadoId = empleadoId,
            ClienteId = clienteId,
            NombreComprador = nombreComprador,
            TelefonoComprador = telefonoComprador,
            EmailComprador = emailComprador,
            MetodoPago = metodoPago,
            Fecha = DateTime.UtcNow,
            Total = total,
            EsCotizacion = esCotizacion,
            Estado = estado
        };
    }
}
