using StockManager.Domain.Exceptions;

namespace StockManager.Domain.Entities;

/// <summary>
/// Préstamo en cuotas o adelanto de sueldo a un trabajador. La cuota se descuenta en cada
/// quincena (ver CuotaPrestamo) hasta saldar la deuda. Un adelanto es un préstamo de una cuota.
/// </summary>
public class Prestamo
{
    public const string TipoPrestamo = "Prestamo";
    public const string TipoAdelanto = "Adelanto";

    public const string EstadoActivo = "Activo";
    public const string EstadoSaldado = "Saldado";
    public const string EstadoCancelado = "Cancelado";

    public const int MaximoCuotas = 24;

    public int Id { get; private set; }
    public int TrabajadorId { get; private set; }
    public string Tipo { get; private set; } = null!;
    public decimal Monto { get; private set; }
    public int NumeroCuotas { get; private set; }
    public decimal ValorCuota { get; private set; }

    /// <summary>Primer día de la quincena desde la que se empieza a descontar (día de calendario).</summary>
    public DateTime PrimeraCuotaDesde { get; private set; }

    public int CuotasDescontadas { get; private set; }
    public decimal SaldoPendiente { get; private set; }
    public string Estado { get; private set; } = EstadoActivo;
    public string? Nota { get; private set; }
    public DateTime FechaCreacion { get; private set; }

    private Prestamo() { }

    public static Prestamo Crear(
        int trabajadorId, string tipo, decimal monto, int numeroCuotas,
        DateTime primeraCuotaDesde, string? nota)
    {
        if (trabajadorId <= 0)
            throw new ArgumentException("El trabajador no es válido.", nameof(trabajadorId));
        if (tipo != TipoPrestamo && tipo != TipoAdelanto)
            throw new ArgumentException("El tipo debe ser préstamo o adelanto.", nameof(tipo));
        if (monto <= 0)
            throw new ArgumentException("El monto debe ser mayor que cero.", nameof(monto));
        if (!string.IsNullOrWhiteSpace(nota) && nota.Trim().Length > 300)
            throw new ArgumentException("La nota no puede exceder 300 caracteres.", nameof(nota));

        // Un adelanto se descuenta completo en una sola quincena.
        var cuotas = tipo == TipoAdelanto ? 1 : numeroCuotas;
        if (cuotas < 1 || cuotas > MaximoCuotas)
            throw new ArgumentException($"El número de cuotas debe estar entre 1 y {MaximoCuotas}.", nameof(numeroCuotas));

        return new Prestamo
        {
            TrabajadorId = trabajadorId,
            Tipo = tipo,
            Monto = monto,
            NumeroCuotas = cuotas,
            // Cuota entera en pesos; la última se ajusta al saldo que quede (ver CuotaPendiente).
            ValorCuota = Math.Ceiling(monto / cuotas),
            PrimeraCuotaDesde = primeraCuotaDesde.Date,
            CuotasDescontadas = 0,
            SaldoPendiente = monto,
            Estado = EstadoActivo,
            Nota = string.IsNullOrWhiteSpace(nota) ? null : nota.Trim(),
            FechaCreacion = DateTime.UtcNow
        };
    }

    /// <summary>Lo que corresponde descontar en la próxima quincena (nunca más que el saldo).</summary>
    public decimal CuotaPendiente() =>
        Estado == EstadoActivo ? Math.Min(ValorCuota, SaldoPendiente) : 0m;

    /// <summary>Aplica un descuento hecho en una quincena ya cerrada.</summary>
    public void RegistrarDescuento(decimal valor)
    {
        if (Estado != EstadoActivo)
            throw new PrestamoEstadoInvalidoException("Solo un préstamo activo recibe descuentos.");
        if (valor <= 0)
            throw new ArgumentException("El valor descontado debe ser mayor que cero.", nameof(valor));

        valor = Math.Min(valor, SaldoPendiente);
        SaldoPendiente -= valor;

        // Una cuota parcial (la nómina no alcanzó) no cuenta como cuota completa.
        if (valor >= ValorCuota || SaldoPendiente == 0)
            CuotasDescontadas++;

        if (SaldoPendiente == 0)
            Estado = EstadoSaldado;
    }

    public void Cancelar()
    {
        if (Estado != EstadoActivo)
            throw new PrestamoEstadoInvalidoException("Solo se puede cancelar un préstamo activo.");
        if (CuotasDescontadas > 0)
            throw new PrestamoEstadoInvalidoException("El préstamo ya tiene cuotas descontadas y no se puede cancelar.");

        Estado = EstadoCancelado;
    }
}
