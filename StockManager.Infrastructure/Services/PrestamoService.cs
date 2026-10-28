using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Common;
using StockManager.Domain.Entities;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

public class PrestamoService : IPrestamoService
{
    private readonly AppDbContext _dbContext;
    private readonly IPeriodoNominaService _periodoService;

    public PrestamoService(AppDbContext dbContext, IPeriodoNominaService periodoService)
    {
        _dbContext = dbContext;
        _periodoService = periodoService;
    }

    public async Task<IReadOnlyList<PrestamoResponse>> ListarAsync(string? estado, int? trabajadorId)
    {
        var consulta = _dbContext.Prestamos.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(estado))
            consulta = consulta.Where(p => p.Estado == estado);
        if (trabajadorId.HasValue)
            consulta = consulta.Where(p => p.TrabajadorId == trabajadorId.Value);

        var prestamos = await consulta
            .OrderBy(p => p.Estado == Prestamo.EstadoActivo ? 0 : 1)
            .ThenByDescending(p => p.FechaCreacion)
            .ThenByDescending(p => p.Id)
            .ToListAsync();

        var nombres = await ObtenerNombresAsync(prestamos.Select(p => p.TrabajadorId));
        return prestamos.Select(p => Mapear(p, nombres.GetValueOrDefault(p.TrabajadorId, ""))).ToList();
    }

    public async Task<PrestamoResponse> CrearAsync(CrearPrestamoRequest request)
    {
        var trabajador = await _dbContext.Trabajadores
            .FirstOrDefaultAsync(t => t.Id == request.TrabajadorId)
            ?? throw new TrabajadorNoEncontradoException(request.TrabajadorId);
        if (!trabajador.Activo)
            throw new ArgumentException("No se puede dar un préstamo a un trabajador inactivo.");

        var primeraCuota = await PrimeraQuincenaDescontableAsync(request.PrimeraCuotaDesde);

        var prestamo = Prestamo.Crear(
            trabajador.Id, request.Tipo, request.Monto, request.NumeroCuotas, primeraCuota, request.Nota);

        _dbContext.Prestamos.Add(prestamo);
        await _dbContext.SaveChangesAsync();

        // Si hay una quincena en borrador, la cuota nueva debe aparecer en ella.
        await _periodoService.RecalcularBorradoresAsync();
        return Mapear(prestamo, trabajador.Nombre);
    }

    public async Task<PrestamoResponse> CancelarAsync(int id)
    {
        var prestamo = await _dbContext.Prestamos.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new PrestamoNoEncontradoException(id);

        // Una cuota ya incluida en una quincena aprobada o pagada se aplicará al cerrarla: si el
        // préstamo se cancelara antes, ese cierre fallaría.
        var cuotaCongelada = await (
            from c in _dbContext.CuotasPrestamo
            join p in _dbContext.PeriodosNomina on c.PeriodoNominaId equals p.Id
            where c.PrestamoId == id && !c.Aplicada && p.Estado != PeriodoNomina.EstadoBorrador
            select c.Id).AnyAsync();
        if (cuotaCongelada)
            throw new PrestamoEstadoInvalidoException(
                "El préstamo tiene una cuota en una quincena ya aprobada. Cierra esa quincena antes de cancelarlo.");

        prestamo.Cancelar();
        await _dbContext.SaveChangesAsync();

        await _periodoService.RecalcularBorradoresAsync();

        var nombres = await ObtenerNombresAsync([prestamo.TrabajadorId]);
        return Mapear(prestamo, nombres.GetValueOrDefault(prestamo.TrabajadorId, ""));
    }

    /// <summary>
    /// Primer día de la quincena indicada, pero nunca dentro de una quincena que ya no admite
    /// cambios (aprobada, pagada o cerrada): en ese caso la primera cuota pasa a la siguiente.
    /// </summary>
    private async Task<DateTime> PrimeraQuincenaDescontableAsync(DateTime dia)
    {
        var inicio = PeriodoNomina.QuincenaDe(dia).Inicio;

        var ultimaCongelada = await _dbContext.PeriodosNomina
            .Where(p => p.Estado != PeriodoNomina.EstadoBorrador)
            .MaxAsync(p => (DateTime?)p.FechaFin);

        if (ultimaCongelada.HasValue && inicio <= ultimaCongelada.Value)
            inicio = PeriodoNomina.QuincenaDe(ultimaCongelada.Value.AddDays(1)).Inicio;

        return inicio;
    }

    private async Task<Dictionary<int, string>> ObtenerNombresAsync(IEnumerable<int> trabajadorIds)
    {
        var ids = trabajadorIds.Distinct().ToList();
        return await _dbContext.Trabajadores
            .AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Nombre);
    }

    internal static PrestamoResponse Mapear(Prestamo p, string nombreTrabajador) => new(
        p.Id,
        p.TrabajadorId,
        nombreTrabajador,
        p.Tipo,
        p.Monto,
        p.NumeroCuotas,
        p.ValorCuota,
        p.PrimeraCuotaDesde,
        p.CuotasDescontadas,
        p.SaldoPendiente,
        p.Estado,
        p.Nota,
        p.FechaCreacion,
        p.CuotaPendiente());
}
