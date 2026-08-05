using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
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

        return MapearAResponse(configuracion.TarifaIvaPorDefecto);
    }

    public async Task<ConfiguracionResponse> ActualizarAsync(ActualizarConfiguracionRequest request)
    {
        var configuracion = await _dbContext.Configuraciones.SingleAsync();
        configuracion.ActualizarTarifaIva(request.TarifaIvaPorDefecto);

        await _dbContext.SaveChangesAsync();

        return MapearAResponse(configuracion.TarifaIvaPorDefecto);
    }

    private static ConfiguracionResponse MapearAResponse(decimal tarifaIvaPorDefecto)
    {
        return new ConfiguracionResponse
        {
            TarifaIvaPorDefecto = tarifaIvaPorDefecto
        };
    }
}
