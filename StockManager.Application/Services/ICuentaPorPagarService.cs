using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface ICuentaPorPagarService
{
    Task<CuentaPorPagarResponse> CrearAsync(CrearCuentaPorPagarRequest request);
    Task<CuentaPorPagarResponse> RegistrarAbonoAsync(int cuentaId, RegistrarAbonoCuentaPorPagarRequest request, int empleadoId);
    Task<CuentaPorPagarResponse> CancelarAsync(int cuentaId);
    Task<CuentaPorPagarResponse?> ObtenerPorIdAsync(int id);
    Task<(List<CuentaPorPagarResumenResponse> Items, int Total)> ObtenerPaginadoAsync(
        int pagina, int tamanoPagina, string? estado, int? proveedorId);

    /// <summary>Cuentas Pendientes cuya FechaVencimiento cae dentro de los próximos <paramref name="dias"/> días, o ya vencidas. Para el Dashboard.</summary>
    Task<List<CuentaPorPagarResumenResponse>> ObtenerProximasAVencerAsync(int dias);
}
