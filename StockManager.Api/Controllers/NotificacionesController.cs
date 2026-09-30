using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.Services;
using StockManager.Domain.Exceptions;

namespace StockManager.Api.Controllers;

/// <summary>
/// Notificaciones internas para la "campana" del panel (distinto de los envíos de WhatsApp).
/// </summary>
[ApiController]
[Route("api/notificaciones")]
[Authorize(Roles = "Admin,Empleado")]
public class NotificacionesController : ControllerBase
{
    private readonly INotificacionInternaService _notificacionService;

    public NotificacionesController(INotificacionInternaService notificacionService)
    {
        _notificacionService = notificacionService;
    }

    /// <summary>
    /// Lista notificaciones, más recientes primero. Por defecto solo las no leídas.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Obtener([FromQuery] bool soloNoLeidas = true, [FromQuery] int limite = 50)
    {
        var notificaciones = await _notificacionService.ObtenerAsync(soloNoLeidas, limite);
        return Ok(notificaciones);
    }

    /// <summary>
    /// Conteo de notificaciones no leídas, para el badge de la campana.
    /// </summary>
    [HttpGet("conteo-no-leidas")]
    public async Task<IActionResult> ObtenerConteoNoLeidas()
    {
        var conteo = await _notificacionService.ObtenerConteoNoLeidasAsync();
        return Ok(new { conteo });
    }

    [HttpPatch("{id}/marcar-leida")]
    public async Task<IActionResult> MarcarLeida(int id)
    {
        try
        {
            await _notificacionService.MarcarLeidaAsync(id);
            return NoContent();
        }
        catch (NotificacionInternaNoEncontradaException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("marcar-todas-leidas")]
    public async Task<IActionResult> MarcarTodasLeidas()
    {
        await _notificacionService.MarcarTodasLeidasAsync();
        return NoContent();
    }
}
