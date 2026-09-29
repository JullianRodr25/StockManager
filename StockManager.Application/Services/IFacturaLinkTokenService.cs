namespace StockManager.Application.Services;

/// <summary>
/// Genera y valida el token firmado que protege el endpoint público de descarga del PDF
/// de una factura (GET /api/facturas/{id}/pdf?t=...). Ese endpoint no puede exigir el JWT
/// normal porque Twilio necesita poder descargar el archivo él mismo para adjuntarlo al
/// mensaje de WhatsApp; en su lugar, el propio backend genera un token de un solo
/// propósito (atado a esa factura, con vencimiento corto) al despachar la notificación.
/// </summary>
public interface IFacturaLinkTokenService
{
    string GenerarToken(int facturaId, TimeSpan vigencia);

    bool ValidarToken(int facturaId, string token);
}
