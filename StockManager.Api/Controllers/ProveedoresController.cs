using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Constants;
using StockManager.Domain.Exceptions;

namespace StockManager.Api.Controllers;

[ApiController]
[Route("api/proveedores")]
// Nivel de clase = el más amplio (lo mínimo para entrar). Cada acción que no sea la lista lo
// endurece con su propio [Authorize] (se combinan: hay que cumplir los dos), así que si alguien
// agrega un endpoint y olvida el atributo queda de solo lectura, nunca abierto a escritura sin
// querer ni anónimo. El encargado de inventario solo necesita ver la lista para elegir proveedor.
[Authorize(Roles = Roles.PersonalConInventario)]
public class ProveedoresController : ControllerBase
{
    private readonly IProveedorService _proveedorService;

    public ProveedoresController(IProveedorService proveedorService)
    {
        _proveedorService = proveedorService;
    }

    /// <summary>
    /// Registra un nuevo proveedor.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.AdminYEmpleado)]
    public async Task<IActionResult> Crear([FromBody] CrearProveedorRequest request)
    {
        try
        {
            var proveedor = await _proveedorService.CrearAsync(request);
            return Ok(proveedor);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
        {
            return StatusCode(500, new { message = "Error al crear el proveedor" });
        }
    }

    /// <summary>
    /// Obtiene una lista paginada de proveedores, opcionalmente filtrada por si están activos.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerProveedores(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        [FromQuery] bool? activo = null)
    {
        var (items, total) = await _proveedorService.ObtenerPaginadoAsync(pagina, tamanoPagina, activo);

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
    /// Obtiene un proveedor por su ID.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = Roles.AdminYEmpleado)]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var proveedor = await _proveedorService.ObtenerPorIdAsync(id);
        if (proveedor == null)
            return NotFound(new { message = "Proveedor no encontrado" });

        return Ok(proveedor);
    }

    /// <summary>
    /// Actualiza los datos de contacto de un proveedor.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = Roles.AdminYEmpleado)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarProveedorRequest request)
    {
        try
        {
            var proveedor = await _proveedorService.ActualizarAsync(id, request);
            return Ok(proveedor);
        }
        catch (ProveedorNoEncontradoException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
        {
            return StatusCode(500, new { message = "Error al actualizar el proveedor" });
        }
    }

    /// <summary>
    /// Desactiva un proveedor (no se borran proveedores, para conservar el historial de
    /// cuentas por pagar asociadas).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.AdminYEmpleado)]
    public async Task<IActionResult> Desactivar(int id)
    {
        try
        {
            var proveedor = await _proveedorService.DesactivarAsync(id);
            return Ok(proveedor);
        }
        catch (ProveedorNoEncontradoException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
        {
            return StatusCode(500, new { message = "Error al desactivar el proveedor" });
        }
    }

    /// <summary>
    /// Reactiva un proveedor previamente desactivado.
    /// </summary>
    [HttpPost("{id}/activar")]
    [Authorize(Roles = Roles.AdminYEmpleado)]
    public async Task<IActionResult> Activar(int id)
    {
        try
        {
            var proveedor = await _proveedorService.ActivarAsync(id);
            return Ok(proveedor);
        }
        catch (ProveedorNoEncontradoException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
        {
            return StatusCode(500, new { message = "Error al activar el proveedor" });
        }
    }
}
