using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Exceptions;

namespace StockManager.Api.Controllers;

[ApiController]
[Route("api/cuentas-por-pagar")]
[Authorize(Roles = "Admin,Empleado")]
public class CuentasPorPagarController : ControllerBase
{
    private readonly ICuentaPorPagarService _cuentaPorPagarService;

    public CuentasPorPagarController(ICuentaPorPagarService cuentaPorPagarService)
    {
        _cuentaPorPagarService = cuentaPorPagarService;
    }

    /// <summary>
    /// Registra una compra a crédito con un proveedor.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearCuentaPorPagarRequest request)
    {
        try
        {
            var cuenta = await _cuentaPorPagarService.CrearAsync(request);
            return Ok(cuenta);
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
            return StatusCode(500, new { message = "Error al crear la cuenta por pagar" });
        }
    }

    /// <summary>
    /// Obtiene una lista paginada de cuentas por pagar, con filtros opcionales de estado y proveedor.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerCuentas(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? estado = null,
        [FromQuery] int? proveedorId = null)
    {
        var (items, total) = await _cuentaPorPagarService.ObtenerPaginadoAsync(pagina, tamanoPagina, estado, proveedorId);

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
    /// Cuentas Pendientes que vencen dentro de los próximos <paramref name="dias"/> días, o ya
    /// vencidas. Pensado para la sección de alertas del Dashboard del admin.
    /// </summary>
    [HttpGet("proximas-a-vencer")]
    public async Task<IActionResult> ObtenerProximasAVencer([FromQuery] int dias = 7)
    {
        var cuentas = await _cuentaPorPagarService.ObtenerProximasAVencerAsync(dias);
        return Ok(cuentas);
    }

    /// <summary>
    /// Obtiene una cuenta por pagar por su ID, incluyendo el historial de abonos.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var cuenta = await _cuentaPorPagarService.ObtenerPorIdAsync(id);
        if (cuenta == null)
            return NotFound(new { message = "Cuenta por pagar no encontrada" });

        return Ok(cuenta);
    }

    /// <summary>
    /// Registra un abono (pago parcial o total) a una cuenta por pagar. Si el abono cubre el
    /// saldo pendiente, la cuenta se marca Pagada automáticamente.
    /// </summary>
    [HttpPost("{id}/abonos")]
    public async Task<IActionResult> RegistrarAbono(int id, [FromBody] RegistrarAbonoCuentaPorPagarRequest request)
    {
        try
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var cuenta = await _cuentaPorPagarService.RegistrarAbonoAsync(id, request, empleadoId);
            return Ok(cuenta);
        }
        catch (CuentaPorPagarNoEncontradaException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (CuentaPorPagarEstadoInvalidoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
        {
            return StatusCode(500, new { message = "Error al registrar el abono" });
        }
    }

    /// <summary>
    /// Cancela una cuenta por pagar sin abonos registrados (ej. la compra se devolvió).
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancelar(int id)
    {
        try
        {
            var cuenta = await _cuentaPorPagarService.CancelarAsync(id);
            return Ok(cuenta);
        }
        catch (CuentaPorPagarNoEncontradaException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (CuentaPorPagarEstadoInvalidoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (OperacionInvalidaCuentaPorPagarException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
        {
            return StatusCode(500, new { message = "Error al cancelar la cuenta por pagar" });
        }
    }
}
