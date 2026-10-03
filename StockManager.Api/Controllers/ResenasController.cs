using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Exceptions;

namespace StockManager.Api.Controllers;

/// <summary>
/// Reseñas de producto del catálogo público (PWA de clientes). Anidado bajo
/// /api/catalogo/{productoId}/resenas porque una reseña no existe sin su producto — mismo
/// criterio de anidamiento que /api/mi-cuenta para las operaciones del cliente autenticado.
/// </summary>
[ApiController]
[Route("api/catalogo/{productoId}/resenas")]
[Authorize(Roles = "Cliente")]
public class ResenasController : ControllerBase
{
    private readonly IResenaService _resenaService;

    public ResenasController(IResenaService resenaService)
    {
        _resenaService = resenaService;
    }

    private int ClienteIdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Lista las reseñas de un producto, más recientes primero.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListarResenas(int productoId)
    {
        var resenas = await _resenaService.ListarResenasAsync(productoId, ClienteIdActual);
        return Ok(resenas);
    }

    /// <summary>
    /// Crea la reseña del cliente autenticado sobre este producto. Falla si ya tiene una
    /// (debe editarla en vez de crear otra).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CrearResena(int productoId, [FromBody] CrearResenaRequest request)
    {
        try
        {
            var resena = await _resenaService.CrearResenaAsync(productoId, ClienteIdActual, request);
            return Ok(resena);
        }
        catch (ProductoNoEncontradoException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ResenaDuplicadaException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error al crear la reseña" });
        }
    }

    /// <summary>
    /// Edita la reseña del cliente autenticado (debe ser la suya).
    /// </summary>
    [HttpPut("{resenaId}")]
    public async Task<IActionResult> EditarResena(int productoId, int resenaId, [FromBody] EditarResenaRequest request)
    {
        try
        {
            var resena = await _resenaService.EditarResenaAsync(resenaId, ClienteIdActual, request);
            return Ok(resena);
        }
        catch (ResenaNoEncontradaException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error al editar la reseña" });
        }
    }

    /// <summary>
    /// Elimina la reseña del cliente autenticado (debe ser la suya).
    /// </summary>
    [HttpDelete("{resenaId}")]
    public async Task<IActionResult> EliminarResena(int productoId, int resenaId)
    {
        try
        {
            await _resenaService.EliminarResenaAsync(resenaId, ClienteIdActual);
            return NoContent();
        }
        catch (ResenaNoEncontradaException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error al eliminar la reseña" });
        }
    }
}
