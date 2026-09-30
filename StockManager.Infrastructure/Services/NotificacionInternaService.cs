using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

public class NotificacionInternaService : INotificacionInternaService
{
    private readonly AppDbContext _dbContext;

    public NotificacionInternaService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<NotificacionInternaResponse>> ObtenerAsync(bool soloNoLeidas, int limite = 50)
    {
        var query = _dbContext.NotificacionesInternas.AsNoTracking().AsQueryable();

        if (soloNoLeidas)
            query = query.Where(n => !n.Leida);

        return await query
            .OrderByDescending(n => n.FechaCreacion)
            .Take(limite)
            .Select(n => new NotificacionInternaResponse(
                n.Id, n.Tipo, n.Titulo, n.Mensaje, n.EntidadTipo, n.EntidadId, n.FechaCreacion, n.Leida))
            .ToListAsync();
    }

    public async Task<int> ObtenerConteoNoLeidasAsync()
    {
        return await _dbContext.NotificacionesInternas.CountAsync(n => !n.Leida);
    }

    public async Task<NotificacionInternaResponse> CrearAsync(string tipo, string titulo, string mensaje, string entidadTipo, int entidadId)
    {
        var notificacion = NotificacionInterna.Crear(tipo, titulo, mensaje, entidadTipo, entidadId);

        _dbContext.NotificacionesInternas.Add(notificacion);
        await _dbContext.SaveChangesAsync();

        return new NotificacionInternaResponse(
            notificacion.Id, notificacion.Tipo, notificacion.Titulo, notificacion.Mensaje,
            notificacion.EntidadTipo, notificacion.EntidadId, notificacion.FechaCreacion, notificacion.Leida);
    }

    public async Task MarcarLeidaAsync(int id)
    {
        var notificacion = await _dbContext.NotificacionesInternas.FirstOrDefaultAsync(n => n.Id == id);
        if (notificacion == null)
            throw new NotificacionInternaNoEncontradaException(id);

        notificacion.MarcarLeida();
        await _dbContext.SaveChangesAsync();
    }

    public async Task MarcarTodasLeidasAsync()
    {
        // ExecuteUpdate evita traer todas las filas a memoria solo para marcarlas; se pierde
        // FechaLeida individual (queda null), aceptable porque no se usa para nada crítico,
        // solo informativo.
        await _dbContext.NotificacionesInternas
            .Where(n => !n.Leida)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Leida, true));
    }
}
