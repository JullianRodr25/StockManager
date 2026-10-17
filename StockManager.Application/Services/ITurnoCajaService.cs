using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface ITurnoCajaService
{
    /// <summary>Abre el turno del empleado con su base inicial; falla si ya tiene uno abierto.</summary>
    Task<TurnoCajaResponse> AbrirAsync(int empleadoId, AbrirTurnoRequest request);

    /// <summary>Turno abierto del empleado (con totales en vivo según la regla de conteo a ciegas), o null.</summary>
    Task<TurnoCajaResponse?> ObtenerTurnoActualAsync(int empleadoId, bool esAdmin);

    /// <summary>Cierra el turno abierto del empleado con el conteo físico y devuelve el cierre completo.</summary>
    Task<TurnoCajaResponse> CerrarAsync(int empleadoId, CerrarTurnoRequest request);

    /// <summary>Un Admin cierra el turno abierto de otro empleado (ej. se fue sin cerrar).</summary>
    Task<TurnoCajaResponse> ForzarCierreAsync(int turnoId, int adminId, CerrarTurnoRequest request);

    /// <summary>Un turno por ID. Un no-Admin solo puede ver los suyos.</summary>
    Task<TurnoCajaResponse> ObtenerPorIdAsync(int turnoId, int usuarioId, bool esAdmin);

    /// <summary>Historial de turnos, el más reciente primero. Un no-Admin solo ve los suyos.</summary>
    Task<(List<TurnoCajaResumenResponse> Items, int Total)> ObtenerHistorialAsync(
        int usuarioId, bool esAdmin, int pagina, int tamanoPagina);

    /// <summary>
    /// Devuelve el turno abierto del empleado al que se debe asociar un pago, o null si no tiene
    /// uno y el pago no es en efectivo. Si el pago incluye efectivo y no hay turno abierto,
    /// lanza TurnoCajaNoAbiertoException: el efectivo siempre debe quedar en un turno.
    /// </summary>
    Task<int?> ResolverTurnoParaPagoAsync(int empleadoId, bool incluyeEfectivo);
}
