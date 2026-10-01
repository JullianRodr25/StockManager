namespace StockManager.Domain.Entities;

using StockManager.Domain.Exceptions;

/// <summary>
/// Entidad Venta del dominio.
/// Representa una venta realizada en el mostrador, una cotización o una cuenta fiada.
/// </summary>
public class Venta
{
    private static readonly string[] MetodosPagoValidos = { "Efectivo", "Tarjeta", "Transferencia", "Mixto" };

    /// <summary>
    /// Métodos que puede llevar una línea individual del desglose de un pago "Mixto". "Mixto"
    /// en sí mismo no es un método individual válido — no tendría sentido una línea "Mixto"
    /// dentro del propio desglose de un pago Mixto.
    /// </summary>
    private static readonly string[] MetodosPagoIndividualesValidos = { "Efectivo", "Tarjeta", "Transferencia" };

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

    /// <summary>
    /// Cuánto efectivo entregó físicamente el cliente, solo cuando MetodoPago es "Efectivo"
    /// (en cualquier otro método queda null: con tarjeta o transferencia no hay "cambio" que
    /// calcular). Se guarda el monto recibido —no directamente el cambio— porque es el dato
    /// que de verdad ocurrió en el mostrador; el cambio (MontoRecibido - Total) se deriva de
    /// ahí, tanto para el tiquete impreso como para una futura conciliación de caja.
    /// </summary>
    public decimal? MontoRecibido { get; private set; }

    /// <summary>Cambio a devolver cuando el pago fue en efectivo; null en cualquier otro método.</summary>
    public decimal? Cambio => MontoRecibido.HasValue ? MontoRecibido.Value - Total : null;

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
        string estado,
        decimal? montoRecibido = null,
        IReadOnlyList<(string MetodoPago, decimal Monto)>? detallesPago = null)
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

        montoRecibido = ValidarMontoRecibido(metodoPago, total, montoRecibido);
        ValidarDetallesPago(metodoPago, total, detallesPago);

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
            Estado = estado,
            MontoRecibido = montoRecibido
        };
    }

    /// <summary>
    /// Si el pago es en efectivo, exige un monto recibido que alcance para cubrir el total (y
    /// lo devuelve tal cual, para que el llamador lo guarde). Para cualquier otro método,
    /// ignora lo que le hayan pasado y siempre devuelve null — "cambio" no es un concepto que
    /// aplique con tarjeta o transferencia, así que no tiene sentido guardarlo ahí.
    /// </summary>
    private static decimal? ValidarMontoRecibido(string metodoPago, decimal total, decimal? montoRecibido)
    {
        if (metodoPago != "Efectivo")
            return null;

        if (montoRecibido is null)
            throw new ArgumentException("Para un pago en efectivo debes indicar el monto recibido.", nameof(montoRecibido));

        if (montoRecibido.Value < total)
            throw new ArgumentException(
                $"El monto recibido ({montoRecibido.Value:F2}) no puede ser menor al total ({total:F2}).",
                nameof(montoRecibido));

        return montoRecibido;
    }

    /// <summary>
    /// Si el pago es "Mixto", exige un desglose de al menos dos líneas (método + monto) cuya
    /// suma sea exactamente igual al total — sin tolerancia: una venta no puede quedar pagada
    /// "casi completa" por un redondeo. Cada línea debe usar un método individual válido
    /// (ningún método puede repetirse como "Mixto" dentro de su propio desglose). Para
    /// cualquier otro método, el desglose no debe venir informado: un pago de un solo método
    /// no tiene nada que desglosar.
    /// </summary>
    private static void ValidarDetallesPago(string metodoPago, decimal total, IReadOnlyList<(string MetodoPago, decimal Monto)>? detallesPago)
    {
        if (metodoPago != "Mixto")
        {
            if (detallesPago is { Count: > 0 })
                throw new ArgumentException("El desglose de pago solo aplica cuando el método es 'Mixto'.", nameof(detallesPago));
            return;
        }

        if (detallesPago is null || detallesPago.Count < 2)
            throw new ArgumentException("Un pago 'Mixto' requiere al menos dos métodos de pago distintos con su monto.", nameof(detallesPago));

        foreach (var (metodoPagoLinea, monto) in detallesPago)
        {
            if (!MetodosPagoIndividualesValidos.Contains(metodoPagoLinea))
                throw new ArgumentException($"El método de pago '{metodoPagoLinea}' no es válido dentro del desglose de un pago Mixto.", nameof(detallesPago));

            if (monto <= 0)
                throw new ArgumentException("Cada línea del desglose de pago debe tener un monto mayor a 0.", nameof(detallesPago));
        }

        var sumaDetalles = detallesPago.Sum(d => d.Monto);
        if (sumaDetalles != total)
            throw new ArgumentException(
                $"La suma del desglose de pago ({sumaDetalles:F2}) debe ser igual al total de la venta ({total:F2}).",
                nameof(detallesPago));
    }

    /// <summary>
    /// Abre una cuenta fiada para un cliente registrado.
    /// Estado queda en "Pendiente" y MetodoPago en null, ya que aún no se sabe cómo se va a pagar.
    /// </summary>
    public static Venta AbrirFiado(int empleadoId, int clienteId, string nombreCliente)
    {
        if (empleadoId <= 0)
            throw new ArgumentException("EmpleadoId debe ser mayor a 0.", nameof(empleadoId));

        if (clienteId <= 0)
            throw new ArgumentException("ClienteId debe ser mayor a 0.", nameof(clienteId));

        if (string.IsNullOrWhiteSpace(nombreCliente))
            throw new ArgumentException("El nombre del cliente es obligatorio.", nameof(nombreCliente));

        return new Venta
        {
            EmpleadoId = empleadoId,
            ClienteId = clienteId,
            NombreComprador = nombreCliente,
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
    public void CerrarFiado(string metodoPago, decimal? montoRecibido = null, IReadOnlyList<(string MetodoPago, decimal Monto)>? detallesPago = null)
    {
        if (Estado != "Pendiente")
            throw new VentaEstadoInvalidoException(Id, Estado, "Pendiente");

        if (!MetodosPagoValidos.Contains(metodoPago))
            throw new ArgumentException($"El método de pago '{metodoPago}' no es válido.", nameof(metodoPago));

        ValidarDetallesPago(metodoPago, Total, detallesPago);

        MetodoPago = metodoPago;
        MontoRecibido = ValidarMontoRecibido(metodoPago, Total, montoRecibido);
        Estado = "Pagada";
    }

    /// <summary>
    /// Cancela por completo una cuenta fiada: exige que esté "Pendiente" y pasa a "Cancelada".
    /// La validación de que no tenga abonos registrados es responsabilidad del servicio de aplicación,
    /// ya que requiere consultar la tabla AbonosCuenta.
    /// </summary>
    public void CancelarCuenta()
    {
        if (Estado != "Pendiente")
            throw new VentaEstadoInvalidoException(Id, Estado, "Pendiente");

        Estado = "Cancelada";
    }
}
