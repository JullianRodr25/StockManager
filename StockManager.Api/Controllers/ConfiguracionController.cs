using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.DTOs;
using StockManager.Application.Services;

namespace StockManager.Api.Controllers;

[ApiController]
[Route("api/configuracion")]
public class ConfiguracionController : ControllerBase
{
    private readonly IConfiguracionService _configuracionService;

    public ConfiguracionController(IConfiguracionService configuracionService)
    {
        _configuracionService = configuracionService;
    }

    /// <summary>
    /// Obtiene la configuración general vigente (tarifa de IVA y teléfono de notificaciones
    /// administrativas por WhatsApp).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Empleado")]
    public async Task<ActionResult<ConfiguracionResponse>> Obtener()
    {
        var configuracion = await _configuracionService.ObtenerAsync();

        // El teléfono de notificaciones administrativas es un dato operativo sensible (a dónde
        // llegan las alertas de pedidos y cuentas por pagar de la tienda): se oculta para
        // Empleado aunque el resto de la configuración (la tarifa de IVA) sí le sea visible,
        // que es justamente el filtrado "por rol" que se pidió para este dato.
        if (!User.IsInRole("Admin"))
            configuracion.TelefonoNotificacionesAdmin = null;

        return Ok(configuracion);
    }

    /// <summary>
    /// Actualiza la configuración general. Restringido a Admin: el teléfono de notificaciones
    /// es un dato sensible desde el punto de vista operativo (a dónde llegan las alertas de
    /// pedidos y cuentas por pagar), así que solo un administrador puede cambiarlo — igual que
    /// la tarifa de IVA, con la que comparte este mismo endpoint.
    /// </summary>
    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ConfiguracionResponse>> Actualizar([FromBody] ActualizarConfiguracionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var configuracion = await _configuracionService.ActualizarAsync(request);
            return Ok(configuracion);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
        {
            return StatusCode(500, new { message = "Error al actualizar la configuración" });
        }
    }
}