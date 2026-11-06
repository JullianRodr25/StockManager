namespace StockManager.Domain.Entities;

/// <summary>
/// Devolución o cambio sobre una venta ya pagada. Es un documento NUEVO ligado a la venta: la
/// venta original nunca se edita ni se borra, porque es un hecho contable y fiscal (si cambiara,
/// el cierre de caja de ese día dejaría de cuadrar).
///
/// Cubre tres casos con el mismo modelo:
/// - Devolución pura: se devuelven líneas y se reembolsa su valor.
/// - Cambio por otro producto: se devuelven líneas y el cliente se lleva otras.
/// - Cambio de precio: se devuelve una línea y se vuelve a entregar a otro precio.
///
/// La plata se resuelve con un único número, <see cref="Diferencia"/>: positivo = el cliente paga,
/// negativo = se le reembolsa, cero = no se mueve dinero.
/// </summary>
public class Devolucion
{
    public const int LongitudMaximaMotivo = 200;

    /// <summary>Métodos con los que se puede pagar o reembolsar la diferencia.</summary>
    public static readonly string[] MetodosPagoValidos = { "Efectivo", "Tarjeta", "Transferencia" };

    public int Id { get; private set; }
    public int VentaId { get; private set; }
    public int EmpleadoId { get; private set; }
    public DateTime Fecha { get; private set; }
    public string Motivo { get; private set; } = null!;

    /// <summary>Valor de lo devuelto, al precio de la venta original (no al de hoy).</summary>
    public decimal TotalDevuelto { get; private set; }

    /// <summary>Valor de lo que el cliente se lleva a cambio, al precio vigente. Cero si fue devolución pura.</summary>
    public decimal TotalCambio { get; private set; }

    /// <summary>Método con el que se pagó o reembolsó la diferencia. Null cuando la diferencia es cero.</summary>
    public string? MetodoPago { get; private set; }

    /// <summary>Turno de caja en el que se movió el dinero. Null si no había turno abierto (solo posible sin efectivo).</summary>
    public int? TurnoCajaId { get; private set; }

    /// <summary>
    /// La venta original tenía factura electrónica: hay que emitir la nota crédito ante la DIAN.
    /// Hoy el sistema no la transmite; esta marca es el recordatorio para no olvidarla.
    /// </summary>
    public bool RequiereNotaCredito { get; private set; }

    /// <summary>Positivo: el cliente paga la diferencia. Negativo: se le reembolsa. Cero: sin movimiento de dinero.</summary>
    public decimal Diferencia => TotalCambio - TotalDevuelto;

    private Devolucion() { }

    public static Devolucion Crear(
        int ventaId,
        int empleadoId,
        string motivo,
        decimal totalDevuelto,
        decimal totalCambio,
        string? metodoPago,
        bool requiereNotaCredito)
    {
        if (ventaId <= 0)
            throw new ArgumentException("VentaId debe ser mayor a 0.", nameof(ventaId));

        if (empleadoId <= 0)
            throw new ArgumentException("EmpleadoId debe ser mayor a 0.", nameof(empleadoId));

        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("Indica el motivo de la devolución.", nameof(motivo));

        if (motivo.Trim().Length > LongitudMaximaMotivo)
            throw new ArgumentException($"El motivo no puede exceder {LongitudMaximaMotivo} caracteres.", nameof(motivo));

        if (totalDevuelto <= 0)
            throw new ArgumentException("Hay que devolver al menos una línea.", nameof(totalDevuelto));

        if (totalCambio < 0)
            throw new ArgumentException("El valor del cambio no puede ser negativo.", nameof(totalCambio));

        var diferencia = totalCambio - totalDevuelto;
        if (diferencia == 0)
        {
            // Cambio por el mismo valor: no se mueve plata, así que no hay método que registrar.
            metodoPago = null;
        }
        else if (string.IsNullOrWhiteSpace(metodoPago) || !MetodosPagoValidos.Contains(metodoPago))
        {
            throw new ArgumentException(
                diferencia > 0
                    ? "Indica con qué método paga el cliente la diferencia."
                    : "Indica con qué método se reembolsa al cliente.",
                nameof(metodoPago));
        }

        return new Devolucion
        {
            VentaId = ventaId,
            EmpleadoId = empleadoId,
            Fecha = DateTime.UtcNow,
            Motivo = motivo.Trim(),
            TotalDevuelto = totalDevuelto,
            TotalCambio = totalCambio,
            MetodoPago = metodoPago,
            RequiereNotaCredito = requiereNotaCredito
        };
    }

    /// <summary>Asocia el movimiento de dinero al turno de caja abierto; una vez asignado no cambia.</summary>
    public void AsignarTurnoCaja(int turnoCajaId)
    {
        if (turnoCajaId <= 0)
            throw new ArgumentException("TurnoCajaId debe ser mayor a 0.", nameof(turnoCajaId));

        if (TurnoCajaId.HasValue && TurnoCajaId.Value != turnoCajaId)
            throw new InvalidOperationException("Esta devolución ya pertenece a otro turno de caja.");

        TurnoCajaId = turnoCajaId;
    }
}
