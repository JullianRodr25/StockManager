using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Common;
using StockManager.Domain.Entities;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

/// <summary>
/// Retiros parciales de efectivo de la caja. Solo registra y consulta retiros: el efecto sobre el
/// efectivo esperado lo aplica TurnoCajaService al calcular los totales del turno, de modo que
/// la fórmula del cierre siga viviendo en un solo lugar.
/// </summary>
public class RetiroCajaService : IRetiroCajaService
{
    private readonly AppDbContext _dbContext;
    private readonly ITurnoCajaService _turnoCajaService;

    public RetiroCajaService(AppDbContext dbContext, ITurnoCajaService turnoCajaService)
    {
        _dbContext = dbContext;
        _turnoCajaService = turnoCajaService;
    }

    public async Task<RetiroCajaResponse> RegistrarAsync(int empleadoId, bool esAdmin, RegistrarRetiroRequest request)
    {
        // esAdmin: true fuerza a que el turno traiga el esperado calculado; si se le puede
        // mostrar a quien consulta lo decide la regla de conteo a ciegas de más abajo.
        var turno = await _turnoCajaService.ObtenerTurnoActualAsync(empleadoId, esAdmin: true)
            ?? throw new OperacionInvalidaCajaException("Para registrar un retiro primero debes abrir tu caja.");

        // Con el conteo a ciegas activo un cajero no debe ver el esperado; validar el tope le
        // permitiría descubrirlo por tanteo ("¿me deja retirar X?"), así que solo se valida
        // cuando quien retira tiene derecho a ver ese dato.
        var conteoCiego = await _dbContext.Configuraciones.Select(c => c.ConteoCiegoCaja).FirstOrDefaultAsync();
        var esperadoVisible = esAdmin || !conteoCiego;
        if (esperadoVisible && turno.EfectivoEsperado.HasValue && request.Monto > turno.EfectivoEsperado.Value)
            throw new OperacionInvalidaCajaException(
                $"El retiro ({request.Monto:N0}) supera el efectivo que debería haber en caja ({turno.EfectivoEsperado.Value:N0}).");

        var retiro = RetiroCaja.Crear(turno.Id, empleadoId, request.Monto);
        _dbContext.RetirosCaja.Add(retiro);
        await _dbContext.SaveChangesAsync();

        return (await MapearAsync(new List<RetiroCaja> { retiro }))[0];
    }

    public async Task<RetiroCajaResponse> AnularAsync(int retiroId, int adminId)
    {
        var retiro = await _dbContext.RetirosCaja.FirstOrDefaultAsync(r => r.Id == retiroId)
            ?? throw new RetiroCajaNoEncontradoException(retiroId);

        var turnoAbierto = await _dbContext.TurnosCaja
            .AnyAsync(t => t.Id == retiro.TurnoCajaId && t.Estado == TurnoCaja.EstadoAbierto);
        if (!turnoAbierto)
            throw new OperacionInvalidaCajaException(
                "El turno de este retiro ya está cerrado; sus totales quedaron firmados y no se puede anular.");

        retiro.Anular(adminId);
        await _dbContext.SaveChangesAsync();

        return (await MapearAsync(new List<RetiroCaja> { retiro }))[0];
    }

    public async Task<List<RetiroCajaResponse>> ObtenerPorTurnoAsync(int turnoId, int usuarioId, bool esAdmin)
    {
        var turno = await _dbContext.TurnosCaja.AsNoTracking().FirstOrDefaultAsync(t => t.Id == turnoId)
            ?? throw new TurnoCajaNoEncontradoException(turnoId);

        // Misma regla que ObtenerPorIdAsync: no se revela la existencia de turnos ajenos.
        if (!esAdmin && turno.EmpleadoId != usuarioId)
            throw new TurnoCajaNoEncontradoException(turnoId);

        var retiros = await _dbContext.RetirosCaja.AsNoTracking()
            .Where(r => r.TurnoCajaId == turnoId)
            .OrderBy(r => r.Fecha)
            .ToListAsync();

        return await MapearAsync(retiros);
    }

    public async Task<ReporteRetirosResponse> ReporteAsync(DateOnly desde, DateOnly hasta, int? empleadoId)
    {
        if (hasta < desde)
            throw new ArgumentException("La fecha final no puede ser anterior a la inicial.");

        // Mismo criterio que los demás reportes: los días son de Colombia, los instantes UTC.
        var inicioUtc = HoraColombia.AUtc(desde.ToDateTime(TimeOnly.MinValue));
        var finUtc = HoraColombia.AUtc(hasta.AddDays(1).ToDateTime(TimeOnly.MinValue));

        var consulta = _dbContext.RetirosCaja.AsNoTracking()
            .Where(r => r.Fecha >= inicioUtc && r.Fecha < finUtc);
        if (empleadoId.HasValue)
            consulta = consulta.Where(r => r.EmpleadoId == empleadoId.Value);

        var retiros = await consulta.OrderByDescending(r => r.Fecha).ToListAsync();
        var items = await MapearAsync(retiros);

        var vigentes = retiros.Where(r => !r.Anulado).ToList();
        var porDia = vigentes
            .GroupBy(r => DateOnly.FromDateTime(HoraColombia.ALocal(r.Fecha)))
            .OrderByDescending(g => g.Key)
            .Select(g => new RetirosPorDiaResponse(g.Key, g.Count(), g.Sum(r => r.Monto)))
            .ToList();

        return new ReporteRetirosResponse(desde, hasta, vigentes.Sum(r => r.Monto), vigentes.Count, porDia, items);
    }

    private async Task<List<RetiroCajaResponse>> MapearAsync(List<RetiroCaja> retiros)
    {
        var ids = retiros.Select(r => r.EmpleadoId)
            .Concat(retiros.Where(r => r.AnuladoPorEmpleadoId.HasValue).Select(r => r.AnuladoPorEmpleadoId!.Value))
            .Distinct()
            .ToList();
        var nombres = await _dbContext.Empleados.AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Nombre);

        return retiros.Select(r => new RetiroCajaResponse(
            r.Id,
            r.TurnoCajaId,
            r.EmpleadoId,
            nombres.GetValueOrDefault(r.EmpleadoId, string.Empty),
            r.Fecha,
            r.Monto,
            r.Anulado,
            r.AnuladoPorEmpleadoId.HasValue ? nombres.GetValueOrDefault(r.AnuladoPorEmpleadoId.Value) : null,
            r.FechaAnulacion)).ToList();
    }
}
