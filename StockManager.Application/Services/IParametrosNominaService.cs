using StockManager.Application.DTOs;
using StockManager.Domain.Entities;

namespace StockManager.Application.Services;

public interface IParametrosNominaService
{
    Task<ParametrosNominaResponse> ObtenerAsync();
    Task<ParametrosNominaResponse> ActualizarAsync(ParametrosNominaRequest request);

    /// <summary>Entidad para el cálculo de quincenas. Crea los valores por defecto si aún no existen.</summary>
    Task<ParametrosNomina> ObtenerEntidadAsync();
}
