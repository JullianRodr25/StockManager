namespace StockManager.Application.Services;

/// <summary>
/// Genera el PDF de una Factura a partir de sus datos ya guardados (Venta, líneas y
/// comprador). No hay un archivo almacenado: se regenera cada vez que se pide.
/// </summary>
public interface IFacturaPdfService
{
    /// <summary>Devuelve null si la factura no existe.</summary>
    Task<byte[]?> GenerarPdfAsync(int facturaId);
}
