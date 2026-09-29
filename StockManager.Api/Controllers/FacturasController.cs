using Microsoft.AspNetCore.Mvc;
using StockManager.Application.Services;

namespace StockManager.Api.Controllers;

[ApiController]
[Route("api/facturas")]
public class FacturasController : ControllerBase
{
    private readonly IFacturaPdfService _facturaPdfService;
    private readonly IFacturaLinkTokenService _tokenService;

    public FacturasController(IFacturaPdfService facturaPdfService, IFacturaLinkTokenService tokenService)
    {
        _facturaPdfService = facturaPdfService;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Descarga el PDF de una factura. Deliberadamente SIN [Authorize]: Twilio necesita poder
    /// descargar este archivo él mismo para adjuntarlo al mensaje de WhatsApp, y no puede
    /// presentar el JWT de un usuario logueado. En su lugar, el acceso lo protege el
    /// parámetro "t": un token firmado (HMAC), de un solo propósito (atado a este facturaId)
    /// y de vigencia corta, que solo el propio backend genera al despachar la notificación.
    /// Sin un token válido y vigente, la petición se rechaza igual que si el recurso no existiera.
    /// </summary>
    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> ObtenerPdf(int id, [FromQuery] string t)
    {
        if (!_tokenService.ValidarToken(id, t))
            return NotFound();

        var pdf = await _facturaPdfService.GenerarPdfAsync(id);
        if (pdf is null)
            return NotFound();

        return File(pdf, "application/pdf", $"factura-{id}.pdf");
    }
}
