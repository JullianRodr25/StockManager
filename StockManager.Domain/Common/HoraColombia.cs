namespace StockManager.Domain.Common;

/// <summary>
/// Única fuente de verdad de "qué hora/qué día es" para el negocio (Colombia, UTC-5 fijo: no
/// tiene horario de verano).
///
/// Regla del sistema: los INSTANTES (Fecha de una venta, factura, abono...) se guardan y viajan
/// en UTC; los DÍAS de calendario (fecha de vencimiento, "hoy") son del día en Colombia. Como el
/// servidor (Azure) corre en UTC, <c>DateTime.UtcNow.Date</c> cambia de día a las 7:00 p. m. hora
/// colombiana, así que nunca debe usarse para decidir "hoy": se usa <see cref="Hoy"/> o
/// <see cref="InicioDiaUtc"/>.
/// </summary>
public static class HoraColombia
{
    private static readonly TimeSpan Desfase = TimeSpan.FromHours(-5);

    private static readonly TimeZoneInfo Zona = ResolverZona();

    private static TimeZoneInfo ResolverZona()
    {
        foreach (var id in new[] { "America/Bogota", "SA Pacific Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("Colombia", Desfase, "Colombia", "Colombia");
    }

    /// <summary>Convierte un instante UTC a la hora de pared de Colombia (Kind sin especificar).</summary>
    public static DateTime ALocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zona);

    /// <summary>Convierte una hora de pared de Colombia al instante UTC equivalente.</summary>
    public static DateTime AUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zona);

    /// <summary>El día de calendario actual en Colombia (hora 00:00). Para comparar con fechas "de día", como FechaVencimiento.</summary>
    public static DateTime Hoy(DateTime? ahoraUtc = null) => ALocal(ahoraUtc ?? DateTime.UtcNow).Date;

    /// <summary>
    /// El instante UTC en que empezó el día de hoy en Colombia. Para filtrar instantes
    /// ("ventas de hoy": <c>Fecha &gt;= InicioDiaUtc()</c> y <c>Fecha &lt; InicioDiaUtc().AddDays(1)</c>).
    /// </summary>
    public static DateTime InicioDiaUtc(DateTime? ahoraUtc = null) =>
        DateTime.SpecifyKind(AUtc(Hoy(ahoraUtc)), DateTimeKind.Utc);
}
