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

    /// <summary>
    /// Abre una cuenta fiada (venta en estado Pendiente) para un cliente registrado.
    /// </summary>
    [HttpPost("fiado")]
    [Authorize(Roles = "Admin,Empleado")]
    public async Task<IActionResult> AbrirFiado([FromBody] AbrirFiadoRequest request)
    {
        try
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var venta = await _ventaService.AbrirFiadoAsync(request.ClienteId, empleadoId);
            return Ok(venta);
        }
        catch (CuentaFiadoAbiertaException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error al abrir la cuenta fiada" });
        }
    }

    /// <summary>
    /// Agrega una línea de producto a una cuenta fiada abierta y descuenta el stock de inmediato.
    /// </summary>
    [HttpPost("{id}/lineas")]
    [Authorize(Roles = "Admin,Empleado")]
    public async Task<IActionResult> AgregarLineaFiado(int id, [FromBody] LineaVentaRequest request)
    {
        try
        {
            var venta = await _ventaService.AgregarLineaFiadoAsync(id, request);
            return Ok(venta);
        }
        catch (VentaNoEncontradaException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (VentaEstadoInvalidoException ex)
        {
            return Conflict(new { message = ex.Message });
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
            return StatusCode(500, new { message = "Error al agregar la línea a la cuenta fiada" });
        }
    }

    /// <summary>
    /// Cierra una cuenta fiada: fija el método de pago, pasa la venta a Pagada y genera la Factura.
    /// </summary>
    [HttpPost("{id}/cerrar")]
    [Authorize(Roles = "Admin,Empleado")]
    public async Task<IActionResult> CerrarFiado(int id, [FromBody] CerrarFiadoRequest request)
    {
        try
        {
            var venta = await _ventaService.CerrarFiadoAsync(id, request.MetodoPago);
            return Ok(venta);
        }
        catch (VentaNoEncontradaException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (VentaEstadoInvalidoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error al cerrar la cuenta fiada" });
        }
    }
}