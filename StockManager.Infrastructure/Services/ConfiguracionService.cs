using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

public class ConfiguracionService : IConfiguracionService
{
    private readonly AppDbContext _dbContext;

    public ConfiguracionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ConfiguracionResponse> ObtenerAsync()
    {
        var configuracion = await _dbContext.Configuraciones.SingleAsync();

        return MapearAResponse(configuracion);
    }

    public async Task<ConfiguracionResponse> ActualizarAsync(ActualizarConfiguracionRequest request)
    {
        var configuracion = await _dbContext.Configuraciones.SingleAsync();
        configuracion.ActualizarTarifaIva(request.TarifaIvaPorDefecto);
        configuracion.ActualizarTelefonoNotificacionesAdmin(request.TelefonoNotificacionesAdmin);
        configuracion.ActualizarNombreImpresoraTickets(request.NombreImpresoraTickets);
        configuracion.ActualizarDatosEmpresa(
            request.NombreEmpresa,
            request.NitEmpresa,
            request.DireccionEmpresa,
            request.TelefonoEmpresa,
            request.EmailEmpresa);

        await _dbContext.SaveChangesAsync();

        return MapearAResponse(configuracion);
    }

    private static ConfiguracionResponse MapearAResponse(Configuracion configuracion)
    {
        return new ConfiguracionResponse
        {
            TarifaIvaPorDefecto = configuracion.TarifaIvaPorDefecto,
            TelefonoNotificacionesAdmin = configuracion.TelefonoNotificacionesAdmin,
            NombreImpresoraTickets = configuracion.NombreImpresoraTickets,
            NombreEmpresa = configuracion.NombreEmpresa,
            NitEmpresa = configuracion.NitEmpresa,
            DireccionEmpresa = configuracion.DireccionEmpresa,
            TelefonoEmpresa = configuracion.TelefonoEmpresa,
            EmailEmpresa = configuracion.EmailEmpresa
        };
    }
}
