using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface IReporteService
{
    Task<ReporteVentasResponse> VentasAsync(DateOnly desde, DateOnly hasta);
    Task<ReporteProductosVendidosResponse> ProductosVendidosAsync(DateOnly desde, DateOnly hasta, int top);
    Task<ReporteInventarioResponse> InventarioAsync();
    Task<ReporteCarteraResponse> CarteraAsync();
}
