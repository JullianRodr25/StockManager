using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Exceptions;

namespace StockManager.Api.Controllers;

/// <summary>
/// Autoservicio del Cliente autenticado desde la PWA: ver/editar sus propios datos de
/// contacto y cambiar su propia contraseña. Deliberadamente separado de ClientesController
/// (que es [Authorize(Roles = "Admin,Empleado")] y opera sobre cualquier cliente por ID): acá
/// el ID sale siempre del token, nunca del request, así que un cliente no puede leer ni tocar
/// la cuenta de otro cambiando un parámetro. Ambos controladores reutilizan el mismo
/// IClienteService como única fuente de verdad para las reglas de negocio.
/// </summary>
[ApiController]
[Route("api/mi-cuenta")]
[Authorize(Roles = "Cliente")]
public class MiCuentaController : ControllerBase
{
    private readonly IClienteService _clienteService;

    public MiCuentaController(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    private int ClienteIdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Obtiene los datos del cliente autenticado (pantalla "Mi perfil" de la PWA).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerMiCuenta()
    {
        var cliente = await _clienteService.ObtenerClientePorIdAsync(ClienteIdActual);
        if (cliente == null)
            return NotFound(new { message = "Cliente no encontrado" });

        return Ok(cliente);
    }

    /// <summary>
    /// Actualiza los datos de contacto (nombre, email, teléfono, dirección) del propio
    /// cliente autenticado. Misma validación y reglas que la edición desde el panel
    /// (ClientesController.Actualizar), aplicadas siempre sobre el cliente del token.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> ActualizarMiCuenta([FromBody] ActualizarClienteRequest request)
    {
        try
        {
            var cliente = await _clienteService.ActualizarClienteAsync(ClienteIdActual, request);
            return Ok(cliente);
        }
        catch (ClienteNoEncontradoException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UsuarioDuplicadoPorEmailException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error al actualizar tus datos" });
        }
    }

    /// <summary>
    /// Cambia la contraseña del propio cliente autenticado, exigiendo la contraseña actual.
    /// </summary>
    [HttpPost("cambiar-password")]
    public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordPropioRequest request)
    {
        try
        {
            await _clienteService.CambiarPasswordPropioAsync(ClienteIdActual, request);
            return Ok(new { message = "Contraseña actualizada correctamente" });
        }
        catch (ClienteNoEncontradoException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ContrasenaActualIncorrectaException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error al cambiar la contraseña" });
        }
    }
}
