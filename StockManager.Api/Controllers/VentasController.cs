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

    /// <summary>
    /// Obtiene una lista paginada de ventas, con filtros opcionales de rango de fecha y estado.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Empleado")]
    public async Task<IActionResult> ObtenerVentas(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        [FromQuery] string? estado = null)
    {
        var (items, total) = await _ventaService.ObtenerVentasPaginadoAsync(pagina, tamanoPagina, desde, hasta, estado);

        return Ok(new
        {
            data = items,
            pagina,
            tamanoPagina,
            total,
            totalPaginas = (int)Math.Ceiling((double)total / tamanoPagina)
        });
    }

    /// <summary>
    /// Obtiene una venta por su ID, incluyendo sus detalles y número de factura.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Empleado")]
    public async Task<IActionResult> ObtenerVentaPorId(int id)
    {
        var venta = await _ventaService.ObtenerVentaPorIdAsync(id);
        if (venta == null)
            return NotFound(new { message = "Venta no encontrada" });

        return Ok(venta);
    }
}