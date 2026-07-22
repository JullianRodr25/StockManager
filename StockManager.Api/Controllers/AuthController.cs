using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StockManager.Application.Services;
using StockManager.Application.DTOs;
using StockManager.Domain.Exceptions;

namespace StockManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // ===== LOGIN ENDPOINTS =====

    [HttpPost("login/empleado")]
    public async Task<IActionResult> LoginEmpleado([FromBody] AuthRequest request)
    {
        var token = await _authService.LoginEmpleadoAsync(request.Identificador, request.Password);
        if (string.IsNullOrEmpty(token))
            return Unauthorized(new { message = "Credenciales inválidas" });

        return Ok(new AuthResponse(token));
    }

    [HttpPost("login/cliente")]
    public async Task<IActionResult> LoginCliente([FromBody] AuthRequest request)
    {
        var token = await _authService.LoginClienteAsync(request.Identificador, request.Password);
        if (string.IsNullOrEmpty(token))
            return Unauthorized(new { message = "Credenciales inválidas" });

        return Ok(new AuthResponse(token));
    }

    // ===== REGISTRO ENDPOINTS =====

    /// <summary>
    /// Registra un nuevo empleado. Requiere autenticación con rol "Admin".
    /// Solo un Admin existente puede crear otros empleados.
    /// </summary>
    [HttpPost("registrar/empleado")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegistrarEmpleado([FromBody] RegistrarEmpleadoRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var empleadoId = await _authService.RegistrarEmpleadoAsync(request);
            return CreatedAtAction(nameof(RegistrarEmpleado), new { id = empleadoId }, 
                new RegistrarResponse(empleadoId, "Empleado registrado exitosamente"));
        }
        catch (UsuarioDuplicadoPorIdentificacionException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UsuarioDuplicadoPorEmailException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Log aquí si es necesario
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "Ocurrió un error al registrar el empleado" });
        }
    }

    /// <summary>
    /// Registra un nuevo cliente. Endpoint público, sin autenticación requerida.
    /// Cualquiera puede registrarse como cliente desde la PWA.
    /// </summary>
    [HttpPost("registrar/cliente")]
    [AllowAnonymous]
    public async Task<IActionResult> RegistrarCliente([FromBody] RegistrarClienteRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var clienteId = await _authService.RegistrarClienteAsync(request);
            return CreatedAtAction(nameof(RegistrarCliente), new { id = clienteId }, 
                new RegistrarResponse(clienteId, "Cliente registrado exitosamente"));
        }
        catch (UsuarioDuplicadoPorIdentificacionException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UsuarioDuplicadoPorEmailException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Log aquí si es necesario
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "Ocurrió un error al registrar el cliente" });
        }
    }
}
