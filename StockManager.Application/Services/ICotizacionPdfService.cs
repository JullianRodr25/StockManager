using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

/// <summary>Maqueta el PDF de una cotización; solo presenta los datos que recibe ya armados.</summary>
public interface ICotizacionPdfService
{
    Task<byte[]> GenerarAsync(CotizacionResponse cotizacion);
}
