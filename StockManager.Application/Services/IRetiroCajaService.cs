using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface IRetiroCajaService
{
    /// <summary>Registra un retiro parcial de efectivo en el turno abierto del empleado.</summary>
    Task<RetiroCajaResponse> RegistrarAsync(int empleadoId, bool esAdmin, RegistrarRetiroRequest request);

    /// <summary>Anula un retiro (solo Admin, solo con el turno todavía abierto).</summary>
    Task<RetiroCajaResponse> AnularAsync(int retiroId, int adminId);

    /// <summary>Retiros de un turno. Un no-Admin solo puede consultar los de su propio turno.</summary>
    Task<List<RetiroCajaResponse>> ObtenerPorTurnoAsync(int turnoId, int usuarioId, bool esAdmin);

    /// <summary>Reporte de retiros por rango de días (hora Colombia), opcionalmente de un empleado.</summary>
    Task<ReporteRetirosResponse> ReporteAsync(DateOnly desde, DateOnly hasta, int? empleadoId);
}
