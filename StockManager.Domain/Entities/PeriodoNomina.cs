using StockManager.Domain.Exceptions;

namespace StockManager.Domain.Entities;

/// <summary>
/// Un período de nómina de una frecuencia: semanal (lunes a domingo), quincenal (del 1 al 15 o
/// del 16 al último día del mes) o mensual (el mes completo). Avanza en orden:
/// Borrador (se revisa y se editan novedades) → Aprobada (se congela) → Pagada → Cerrada
/// (se descuentan las cuotas de los préstamos). Nunca retrocede.
/// </summary>
public class PeriodoNomina
{
    public const string EstadoBorrador = "Borrador";
    public const string EstadoAprobada = "Aprobada";
    public const string EstadoPagada = "Pagada";
    public const string EstadoCerrada = "Cerrada";

    public int Id { get; private set; }

    /// <summary>Frecuencia del período: solo se liquidan los trabajadores que pagan con ella.</summary>
    public string Frecuencia { get; private set; } = Trabajador.FrecuenciaQuincenal;

    /// <summary>Primer día del período (día de calendario).</summary>
    public DateTime FechaInicio { get; private set; }

    /// <summary>Último día del período (día de calendario).</summary>
    public DateTime FechaFin { get; private set; }

    public string Estado { get; private set; } = EstadoBorrador;
    public DateTime FechaCreacion { get; private set; }
    public DateTime? AprobadaEn { get; private set; }
    public int? AprobadaPorEmpleadoId { get; private set; }
    public DateTime? PagadaEn { get; private set; }
    public DateTime? CerradaEn { get; private set; }

    private PeriodoNomina() { }

    /// <summary>Primer y último día de la quincena que contiene el día dado.</summary>
    public static (DateTime Inicio, DateTime Fin) QuincenaDe(DateTime dia)
    {
        var d = dia.Date;
        if (d.Day <= 15)
            return (new DateTime(d.Year, d.Month, 1), new DateTime(d.Year, d.Month, 15));

        return (new DateTime(d.Year, d.Month, 16),
                new DateTime(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month)));
    }

    /// <summary>Quincena inmediatamente posterior a la que empieza en <paramref name="inicio"/>.</summary>
    public static (DateTime Inicio, DateTime Fin) QuincenaSiguiente(DateTime inicio) =>
        QuincenaDe(QuincenaDe(inicio).Fin.AddDays(1));

    /// <summary>Primer y último día del período de la frecuencia que contiene el día dado.</summary>
    public static (DateTime Inicio, DateTime Fin) PeriodoDe(string frecuencia, DateTime dia)
    {
        var d = dia.Date;
        switch (frecuencia)
        {
            case Trabajador.FrecuenciaSemanal:
                // Semana de lunes a domingo.
                var inicio = d.AddDays(-(((int)d.DayOfWeek + 6) % 7));
                return (inicio, inicio.AddDays(6));
            case Trabajador.FrecuenciaMensual:
                return (new DateTime(d.Year, d.Month, 1),
                        new DateTime(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month)));
            case Trabajador.FrecuenciaQuincenal:
                return QuincenaDe(d);
            default:
                throw new ArgumentException("La frecuencia de pago no es válida.", nameof(frecuencia));
        }
    }

    /// <summary>Período inmediatamente posterior al que empieza en <paramref name="inicio"/>.</summary>
    public static (DateTime Inicio, DateTime Fin) Siguiente(string frecuencia, DateTime inicio) =>
        PeriodoDe(frecuencia, PeriodoDe(frecuencia, inicio).Fin.AddDays(1));

    public static PeriodoNomina Crear(string frecuencia, DateTime fechaInicio)
    {
        var (inicio, fin) = PeriodoDe(frecuencia, fechaInicio);
        if (fechaInicio.Date != inicio)
            throw new ArgumentException(frecuencia switch
            {
                Trabajador.FrecuenciaSemanal => "La semana debe empezar un lunes.",
                Trabajador.FrecuenciaMensual => "El mes debe empezar el día 1.",
                _ => "La quincena debe empezar el día 1 o el día 16."
            }, nameof(fechaInicio));

        return new PeriodoNomina
        {
            Frecuencia = frecuencia,
            FechaInicio = inicio,
            FechaFin = fin,
            Estado = EstadoBorrador,
            FechaCreacion = DateTime.UtcNow
        };
    }

    public bool EsBorrador => Estado == EstadoBorrador;

    public void Aprobar(int empleadoId)
    {
        ExigirEstado(EstadoBorrador, "aprobar");
        Estado = EstadoAprobada;
        AprobadaEn = DateTime.UtcNow;
        AprobadaPorEmpleadoId = empleadoId;
    }

    public void MarcarPagada()
    {
        ExigirEstado(EstadoAprobada, "pagar");
        Estado = EstadoPagada;
        PagadaEn = DateTime.UtcNow;
    }

    public void Cerrar()
    {
        ExigirEstado(EstadoPagada, "cerrar");
        Estado = EstadoCerrada;
        CerradaEn = DateTime.UtcNow;
    }

    /// <summary>Los datos de una quincena solo se pueden editar mientras es borrador.</summary>
    public void ExigirBorrador()
    {
        if (Estado != EstadoBorrador)
            throw new PeriodoNominaEstadoInvalidoException(
                $"El período está {Estado.ToLowerInvariant()} y ya no se puede modificar.");
    }

    private void ExigirEstado(string requerido, string accion)
    {
        if (Estado != requerido)
            throw new PeriodoNominaEstadoInvalidoException(
                $"No se puede {accion} un período que está {Estado.ToLowerInvariant()}.");
    }
}
