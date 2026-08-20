namespace StockManager.Domain.Entities;

using StockManager.Domain.Exceptions;

/// <summary>
/// Entidad Venta del dominio.
/// Representa una venta realizada en el mostrador, una cotización o una cuenta fiada.
/// </summary>
public class Venta
{
    private static readonly string[] MetodosPagoValidos = { "Efectivo", "Tarjeta", "Transferencia", "Mixto" };

    public int Id { get; private set; }
    public int EmpleadoId { get; private set; }
    public int? ClienteId { get; private set; }
    public string? NombreComprador { get; private set; }
    public string? TelefonoComprador { get; private set; }
    public string? EmailComprador { get; private set; }
    public string? MetodoPago { get; private set; }
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

        if (!MetodosPagoValidos.Contains(metodoPago))
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

    /// <summary>
    /// Abre una cuenta fiada para un cliente registrado.
    /// Estado queda en "Pendiente" y MetodoPago en null, ya que aún no se sabe cómo se va a pagar.
    /// </summary>
    public static Venta AbrirFiado(int empleadoId, int clienteId)
    {
        if (empleadoId <= 0)
            throw new ArgumentException("EmpleadoId debe ser mayor a 0.", nameof(empleadoId));

        if (clienteId <= 0)
            throw new ArgumentException("ClienteId debe ser mayor a 0.", nameof(clienteId));

        return new Venta
        {
            EmpleadoId = empleadoId,
            ClienteId = clienteId,
            MetodoPago = null,
            Fecha = DateTime.UtcNow,
            Total = 0m,
            EsCotizacion = false,
            Estado = "Pendiente"
        };
    }

    /// <summary>
    /// Suma un monto al Total de la cuenta fiada conforme se agregan líneas.
    /// </summary>
    public void AgregarMonto(decimal monto)
    {
        if (monto <= 0)
            throw new ArgumentException("El monto a agregar debe ser mayor a 0.", nameof(monto));

        Total += monto;
    }

    /// <summary>
    /// Resta un monto del Total de la cuenta fiada (edición o eliminación de una línea).
    /// </summary>
    public void RestarMonto(decimal monto)
    {
        if (monto <= 0)
            throw new ArgumentException("El monto a restar debe ser mayor a 0.", nameof(monto));

        if (monto > Total)
            throw new ArgumentException("El monto a restar no puede ser mayor al Total actual.", nameof(monto));

        Total -= monto;
    }

    /// <summary>
    /// Cierra una cuenta fiada: exige que esté "Pendiente", fija el método de pago y pasa a "Pagada".
    /// </summary>
    public void CerrarFiado(string metodoPago)
    {
        if (Estado != "Pendiente")
            throw new VentaEstadoInvalidoException(Id, Estado, "Pendiente");

        if (!MetodosPagoValidos.Contains(metodoPago))
            throw new ArgumentException($"El método de pago '{metodoPago}' no es válido.", nameof(metodoPago));

        MetodoPago = metodoPago;
        Estado = "Pagada";
    }
}
