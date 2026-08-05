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
    /// Obtiene la tarifa de IVA general vigente.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Empleado")]
    public async Task<ActionResult<ConfiguracionResponse>> Obtener()
    {
        var configuracion = await _configuracionService.ObtenerAsync();
        return Ok(configuracion);
    }

    /// <summary>
    /// Actualiza la tarifa de IVA general por defecto.
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
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error al actualizar la configuración" });
        }
    }
}