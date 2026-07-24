using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Exceptions;

namespace StockManager.Api.Controllers;

/// <summary>
/// Controlador para operaciones CRUD de categorías.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CategoriasController : ControllerBase
{
    private readonly ICategoriaService _categoriaService;

    public CategoriasController(ICategoriaService categoriaService)
    {
        _categoriaService = categoriaService;
    }

    /// <summary>
    /// Obtiene todas las categorías (acceso: Admin y Empleado).
    /// </summary>
    /// <response code="200">Lista de categorías obtenida correctamente</response>
    /// <response code="401">No autorizado (token inválido o expirado)</response>
    [HttpGet]
    [Authorize(Roles = "Admin,Empleado")]
    public async Task<ActionResult<List<CategoriaResponse>>> ObtenerTodas()
    {
        var categorias = await _categoriaService.ObtenerTodasAsync();
        return Ok(categorias);
    }

    /// <summary>
    /// Crea una nueva categoría (acceso: solo Admin).
    /// </summary>
    /// <param name="request">Datos de la categoría a crear</param>
    /// <response code="201">Categoría creada exitosamente</response>
    /// <response code="400">Error: categoría duplicada o datos inválidos</response>
    /// <response code="401">No autorizado (token inválido o expirado)</response>
    /// <response code="403">Acceso prohibido (no es Admin)</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoriaResponse>> Crear([FromBody] CrearCategoriaRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var categoriaCreada = await _categoriaService.CrearAsync(request);
            return CreatedAtAction(nameof(ObtenerTodas), new { id = categoriaCreada.Id }, categoriaCreada);
        }
        catch (CategoriaDuplicadaException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // TODO: loguear ex con ILogger cuando se agregue logging
            return StatusCode(500, new { message = "Ocurrió un error al crear la categoría" });
        }
    }
}
