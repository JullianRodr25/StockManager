using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

public class ParametrosNominaService : IParametrosNominaService
{
    private readonly AppDbContext _dbContext;

    public ParametrosNominaService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ParametrosNominaResponse> ObtenerAsync() =>
        Mapear(await ObtenerEntidadAsync());

    public async Task<ParametrosNominaResponse> ActualizarAsync(ParametrosNominaRequest request)
    {
        var parametros = await ObtenerEntidadAsync();
        parametros.Actualizar(
            request.SalarioMinimo,
            request.AuxilioTransporte,
            request.LimiteSalariosAuxilio,
            request.PorcentajeSaludTrabajador,
            request.PorcentajePensionTrabajador);

        await _dbContext.SaveChangesAsync();
        return Mapear(parametros);
    }

    public async Task<ParametrosNomina> ObtenerEntidadAsync()
    {
        var parametros = await _dbContext.ParametrosNomina.OrderBy(p => p.Id).FirstOrDefaultAsync();
        if (parametros is not null)
            return parametros;

        // Primera vez: se crean con los valores de referencia, que el administrador puede ajustar.
        parametros = ParametrosNomina.CrearPorDefecto();
        _dbContext.ParametrosNomina.Add(parametros);
        await _dbContext.SaveChangesAsync();
        return parametros;
    }

    private static ParametrosNominaResponse Mapear(ParametrosNomina p) => new(
        p.SalarioMinimo,
        p.AuxilioTransporte,
        p.LimiteSalariosAuxilio,
        p.PorcentajeSaludTrabajador,
        p.PorcentajePensionTrabajador,
        p.FechaActualizacion);
}
