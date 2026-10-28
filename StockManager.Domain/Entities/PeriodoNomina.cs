using StockManager.Domain.Exceptions;

namespace StockManager.Domain.Entities;

/// <summary>
/// Una quincena de nómina (del 1 al 15, o del 16 al último día del mes). Avanza en orden:
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

    /// <summary>Primer día de la quincena (día de calendario).</summary>
    public DateTime FechaInicio { get; private set; }

    /// <summary>Último día de la quincena (día de calendario).</summary>
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

    public static PeriodoNomina Crear(DateTime fechaInicio)
    {
        var (inicio, fin) = QuincenaDe(fechaInicio);
        if (fechaInicio.Date != inicio)
            throw new ArgumentException("La quincena debe empezar el día 1 o el día 16.", nameof(fechaInicio));

        return new PeriodoNomina
        {
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
                $"La quincena está {Estado.ToLowerInvariant()} y ya no se puede modificar.");
    }

    private void ExigirEstado(string requerido, string accion)
    {
        if (Estado != requerido)
            throw new PeriodoNominaEstadoInvalidoException(
                $"No se puede {accion} una quincena que está {Estado.ToLowerInvariant()}.");
    }
}
