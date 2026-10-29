using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Common;
using StockManager.Domain.Entities;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

public class TrabajadorService : ITrabajadorService
{
    private readonly AppDbContext _dbContext;
    private readonly IPeriodoNominaService _periodoService;

    public TrabajadorService(AppDbContext dbContext, IPeriodoNominaService periodoService)
    {
        _dbContext = dbContext;
        _periodoService = periodoService;
    }

    public async Task<IReadOnlyList<TrabajadorResponse>> ListarAsync(bool incluirInactivos)
    {
        var consulta = _dbContext.Trabajadores.AsNoTracking();
        if (!incluirInactivos)
            consulta = consulta.Where(t => t.Activo);

        var trabajadores = await consulta
            .OrderByDescending(t => t.Activo)
            .ThenBy(t => t.Nombre)
            .ThenBy(t => t.Id)
            .ToListAsync();

        var saldos = await ObtenerSaldosAsync();
        var hoy = HoraColombia.Hoy();
        return trabajadores
            .Select(t => Mapear(t, saldos.GetValueOrDefault(t.Id), hoy))
            .ToList();
    }

    public async Task<TrabajadorResponse> ObtenerAsync(int id)
    {
        var trabajador = await BuscarAsync(id);
        return await MapearAsync(trabajador);
    }

    public async Task<TrabajadorResponse> CrearAsync(GuardarTrabajadorRequest request)
    {
        var trabajador = Trabajador.Crear(
            request.Nombre, request.NumeroIdentificacion, request.Telefono, request.Cargo,
            request.TipoContrato, request.FechaIngreso, request.FechaFinContrato,
            request.SalarioMensual, request.AplicaSeguridadSocial, request.MetodoPago,
            request.FrecuenciaPago);

        await ExigirIdentificacionLibreAsync(trabajador.NumeroIdentificacion, null);

        _dbContext.Trabajadores.Add(trabajador);
        await GuardarAsync(trabajador.NumeroIdentificacion);

        await _periodoService.RecalcularBorradoresAsync();
        return await MapearAsync(trabajador);
    }

    public async Task<TrabajadorResponse> ActualizarAsync(int id, GuardarTrabajadorRequest request)
    {
        var trabajador = await BuscarAsync(id);

        trabajador.Actualizar(
            request.Nombre, request.NumeroIdentificacion, request.Telefono, request.Cargo,
            request.TipoContrato, request.FechaIngreso, request.FechaFinContrato,
            request.SalarioMensual, request.AplicaSeguridadSocial, request.MetodoPago,
            request.FrecuenciaPago);

        await ExigirIdentificacionLibreAsync(trabajador.NumeroIdentificacion, id);
        await GuardarAsync(trabajador.NumeroIdentificacion);

        // Una quincena aprobada o pagada no cambia: solo las que están en borrador.
        await _periodoService.RecalcularBorradoresAsync();
        return await MapearAsync(trabajador);
    }

    public async Task<TrabajadorResponse> CambiarEstadoAsync(int id, bool activo)
    {
        var trabajador = await BuscarAsync(id);
        if (activo)
            trabajador.Activar();
        else
            trabajador.Desactivar();

        await _dbContext.SaveChangesAsync();
        await _periodoService.RecalcularBorradoresAsync();
        return await MapearAsync(trabajador);
    }

    private async Task<Trabajador> BuscarAsync(int id) =>
        await _dbContext.Trabajadores.FirstOrDefaultAsync(t => t.Id == id)
        ?? throw new TrabajadorNoEncontradoException(id);

    private async Task ExigirIdentificacionLibreAsync(string numeroIdentificacion, int? idActual)
    {
        var existe = await _dbContext.Trabajadores
            .AnyAsync(t => t.NumeroIdentificacion == numeroIdentificacion && t.Id != (idActual ?? 0));
        if (existe)
            throw new TrabajadorDuplicadoException(numeroIdentificacion);
    }

    private async Task GuardarAsync(string numeroIdentificacion)
    {
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Dos altas simultáneas con la misma identificación: el índice único dejó pasar solo una.
            throw new TrabajadorDuplicadoException(numeroIdentificacion);
        }
    }

    private async Task<Dictionary<int, decimal>> ObtenerSaldosAsync() =>
        await _dbContext.Prestamos
            .AsNoTracking()
            .Where(p => p.Estado == Prestamo.EstadoActivo)
            .GroupBy(p => p.TrabajadorId)
            .Select(g => new { TrabajadorId = g.Key, Saldo = g.Sum(p => p.SaldoPendiente) })
            .ToDictionaryAsync(x => x.TrabajadorId, x => x.Saldo);

    private async Task<TrabajadorResponse> MapearAsync(Trabajador trabajador)
    {
        var saldo = await _dbContext.Prestamos
            .Where(p => p.TrabajadorId == trabajador.Id && p.Estado == Prestamo.EstadoActivo)
            .SumAsync(p => (decimal?)p.SaldoPendiente) ?? 0m;
        return Mapear(trabajador, saldo, HoraColombia.Hoy());
    }

    internal static TrabajadorResponse Mapear(Trabajador t, decimal saldoPrestamos, DateTime hoy) => new(
        t.Id,
        t.Nombre,
        t.NumeroIdentificacion,
        t.Telefono,
        t.Cargo,
        t.TipoContrato,
        t.FechaIngreso,
        t.FechaFinContrato,
        t.SalarioMensual,
        t.AplicaSeguridadSocial,
        t.MetodoPago,
        t.FrecuenciaPago,
        t.Activo,
        saldoPrestamos,
        t.FechaFinContrato is { } fin ? (int)(fin.Date - hoy).TotalDays : null);
}
