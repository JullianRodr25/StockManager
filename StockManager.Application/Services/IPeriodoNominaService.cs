using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface IPeriodoNominaService
{
    Task<IReadOnlyList<PeriodoNominaResumenResponse>> ListarAsync();
    Task<PeriodoNominaResponse?> ObtenerActualAsync();
    Task<PeriodoNominaResponse> ObtenerAsync(int id);
    Task<PeriodoNominaResponse> AbrirAsync(AbrirPeriodoNominaRequest request);
    Task<PeriodoNominaResponse> RecalcularAsync(int id);
    Task<PeriodoNominaResponse> AprobarAsync(int id, int empleadoId);
    Task<PeriodoNominaResponse> PagarLiquidacionAsync(int id, int liquidacionId, int empleadoId);
    Task<PeriodoNominaResponse> PagarTodasAsync(int id, int empleadoId);
    Task<PeriodoNominaResponse> CerrarAsync(int id);
    Task<PeriodoNominaResponse> AgregarNovedadAsync(int id, AgregarNovedadRequest request);
    Task<PeriodoNominaResponse> EliminarNovedadAsync(int id, int novedadId);

    /// <summary>
    /// Vuelve a calcular las quincenas en borrador. Se llama cuando cambia algo que las afecta
    /// (un sueldo, un préstamo nuevo, un trabajador desactivado).
    /// </summary>
    Task RecalcularBorradoresAsync();
}
