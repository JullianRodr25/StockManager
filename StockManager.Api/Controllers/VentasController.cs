using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Exceptions;

namespace StockManager.Api.Controllers;

[ApiController]
[Route("api/ventas")]
public class VentasController : ControllerBase
{
    private readonly IVentaService _ventaService;

    public VentasController(IVentaService ventaService)
    {
        _ventaService = ventaService;
    }

    /// <summary>
    /// Registra una venta simple de mostrador y descuenta el stock de sus productos.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Empleado")]
    public async Task<IActionResult> RegistrarVenta([FromBody] RegistrarVentaRequest request)
    {
        try
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var venta = await _ventaService.RegistrarVentaAsync(request, empleadoId);
            return Ok(venta);
        }
        catch (StockInsuficienteException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ProductoInactivoException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ConcurrencyException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error al registrar la venta" });
        }
    }
}