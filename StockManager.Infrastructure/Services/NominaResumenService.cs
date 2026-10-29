using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Common;
using StockManager.Domain.Entities;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

public class NominaResumenService : INominaResumenService
{
    // Con cuántos días de antelación se avisa que un contrato está por terminar.
    private const int DiasAvisoContrato = 30;

    // Con cuántos días de antelación se recuerda la prima de servicios.
    private const int DiasAvisoPrima = 20;

    private readonly AppDbContext _dbContext;
    private readonly IPeriodoNominaService _periodoService;

    public NominaResumenService(AppDbContext dbContext, IPeriodoNominaService periodoService)
    {
        _dbContext = dbContext;
        _periodoService = periodoService;
    }

    public async Task<NominaResumenResponse> ObtenerAsync()
    {
        var hoy = HoraColombia.Hoy();

        var activos = await _dbContext.Trabajadores
            .AsNoTracking()
            .Where(t => t.Activo)
            .ToListAsync();

        var prestamosActivos = await _dbContext.Prestamos
            .AsNoTracking()
            .Where(p => p.Estado == Prestamo.EstadoActivo)
            .Select(p => p.SaldoPendiente)
            .ToListAsync();

        var actual = await _periodoService.ObtenerActualAsync();

        var alertas = new List<AlertaNominaResponse>();
        AgregarAlertasDeContrato(alertas, activos, hoy);
        AgregarAlertaDePrima(alertas, activos, hoy);
        if (actual is null)
        {
            var (inicio, fin) = PeriodoNomina.PeriodoDe(Trabajador.FrecuenciaQuincenal, hoy);
            alertas.Add(new AlertaNominaResponse(
                "quincena_sin_abrir", "info",
                "La quincena actual no está abierta",
                $"Quincena del {inicio:dd/MM} al {fin:dd/MM}. Ábrela para calcular los pagos.",
                null));
        }

        return new NominaResumenResponse(
            activos.Count,
            activos.Sum(t => t.SalarioMensual),
            prestamosActivos.Sum(),
            prestamosActivos.Count,
            actual?.Resumen,
            alertas);
    }

    private static void AgregarAlertasDeContrato(List<AlertaNominaResponse> alertas, List<Trabajador> activos, DateTime hoy)
    {
        foreach (var t in activos.Where(t => t.FechaFinContrato.HasValue).OrderBy(t => t.FechaFinContrato))
        {
            var dias = (int)(t.FechaFinContrato!.Value.Date - hoy).TotalDays;
            if (dias < 0)
            {
                alertas.Add(new AlertaNominaResponse(
                    "contrato_vencido", "critica",
                    $"Contrato vencido: {t.Nombre}",
                    $"Terminó el {t.FechaFinContrato:dd/MM/yyyy}. Renuévalo o desactiva al trabajador.",
                    t.Id));
            }
            else if (dias <= DiasAvisoContrato)
            {
                alertas.Add(new AlertaNominaResponse(
                    "contrato_por_vencer", "advertencia",
                    $"Contrato por vencer: {t.Nombre}",
                    dias == 0
                        ? "El contrato termina hoy."
                        : $"El contrato termina en {dias} día{(dias == 1 ? "" : "s")} ({t.FechaFinContrato:dd/MM/yyyy}).",
                    t.Id));
            }
        }
    }

    private static void AgregarAlertaDePrima(List<AlertaNominaResponse> alertas, List<Trabajador> activos, DateTime hoy)
    {
        // La prima aplica a quienes tienen contrato laboral (no a prestación de servicios).
        var conPrima = activos.Count(t => t.TipoContrato != Trabajador.ContratoPrestacionServicios);
        if (conPrima == 0)
            return;

        foreach (var (limite, etiqueta) in new[]
        {
            (new DateTime(hoy.Year, 6, 30), "30 de junio"),
            (new DateTime(hoy.Year, 12, 20), "20 de diciembre")
        })
        {
            var dias = (int)(limite - hoy).TotalDays;
            if (dias < 0 || dias > DiasAvisoPrima)
                continue;

            alertas.Add(new AlertaNominaResponse(
                "prima", "advertencia",
                "Se acerca el pago de la prima",
                $"La prima de servicios se paga a más tardar el {etiqueta} ({conPrima} trabajador{(conPrima == 1 ? "" : "es")}).",
                null));
        }
    }
}
