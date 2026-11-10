using StockManager.Domain.Exceptions;

namespace StockManager.Domain.Entities;

/// <summary>
/// Retiro parcial de efectivo de la caja durante un turno (ej. sacar lo vendido y dejar solo la
/// base). No es un gasto ni una venta: solo reduce el efectivo esperado del turno para que el
/// conteo de cierre cuadre contra lo que realmente quedó en el cajón.
///
/// Es historial contable: no se borra. Un error se corrige anulándolo (solo mientras el turno
/// sigue abierto, porque al cerrar los totales se congelan).
/// </summary>
public class RetiroCaja
{
    public int Id { get; private set; }
    public int TurnoCajaId { get; private set; }
    public int EmpleadoId { get; private set; }
    public DateTime Fecha { get; private set; }
    public decimal Monto { get; private set; }

    public bool Anulado { get; private set; }
    public int? AnuladoPorEmpleadoId { get; private set; }
    public DateTime? FechaAnulacion { get; private set; }

    private RetiroCaja() { }

    public static RetiroCaja Crear(int turnoCajaId, int empleadoId, decimal monto)
    {
        if (turnoCajaId <= 0)
            throw new ArgumentException("TurnoCajaId debe ser mayor a 0.", nameof(turnoCajaId));
        if (empleadoId <= 0)
            throw new ArgumentException("EmpleadoId debe ser mayor a 0.", nameof(empleadoId));
        if (monto <= 0)
            throw new OperacionInvalidaCajaException("El monto del retiro debe ser mayor a 0.");

        return new RetiroCaja
        {
            TurnoCajaId = turnoCajaId,
            EmpleadoId = empleadoId,
            Fecha = DateTime.UtcNow,
            Monto = monto
        };
    }

    public void Anular(int empleadoId)
    {
        if (Anulado)
            throw new OperacionInvalidaCajaException("Este retiro ya fue anulado.");

        Anulado = true;
        AnuladoPorEmpleadoId = empleadoId;
        FechaAnulacion = DateTime.UtcNow;
    }
}
