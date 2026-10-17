using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Constants;

namespace StockManager.Api.Controllers;

/// <summary>
/// Turnos de caja: apertura con base, consulta del turno en curso y cierre con conteo de efectivo.
/// Las reglas de negocio viven en TurnoCaja y TurnoCajaService; los errores de dominio los
/// traduce el ManejadorExcepcionesMiddleware, por eso aquí no hay try/catch.
/// </summary>
[ApiController]
[Route("api/caja")]
[Authorize(Roles = Roles.AdminYEmpleado)]
public class CajaController : ControllerBase
{
    private readonly ITurnoCajaService _turnoCajaService;

    public CajaController(ITurnoCajaService turnoCajaService)
    {
        _turnoCajaService = turnoCajaService;
    }

    private int EmpleadoId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool EsAdmin => User.IsInRole(Roles.Admin);

    /// <summary>Abre el turno del usuario con su base inicial.</summary>
    [HttpPost("abrir")]
    public async Task<IActionResult> Abrir([FromBody] AbrirTurnoRequest request)
    {
        var turno = await _turnoCajaService.AbrirAsync(EmpleadoId, request);
        return Ok(turno);
    }

    /// <summary>Turno abierto del usuario; 204 si no tiene ninguno.</summary>
    [HttpGet("turno-actual")]
    public async Task<IActionResult> ObtenerTurnoActual()
    {
        var turno = await _turnoCajaService.ObtenerTurnoActualAsync(EmpleadoId, EsAdmin);
        return turno is null ? NoContent() : Ok(turno);
    }

    /// <summary>Cierra el turno abierto del usuario con el conteo físico de efectivo.</summary>
    [HttpPost("cerrar")]
    public async Task<IActionResult> Cerrar([FromBody] CerrarTurnoRequest request)
    {
        var cierre = await _turnoCajaService.CerrarAsync(EmpleadoId, request);
        return Ok(cierre);
    }

    /// <summary>Un administrador cierra el turno abierto de otro empleado.</summary>
    [HttpPost("{id:int}/forzar-cierre")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ForzarCierre(int id, [FromBody] CerrarTurnoRequest request)
    {
        var cierre = await _turnoCajaService.ForzarCierreAsync(id, EmpleadoId, request);
        return Ok(cierre);
    }

    /// <summary>Un turno por ID (para reimprimir el cierre). Un empleado solo ve los suyos.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var turno = await _turnoCajaService.ObtenerPorIdAsync(id, EmpleadoId, EsAdmin);
        return Ok(turno);
    }

    /// <summary>Historial de turnos, el más reciente primero. Un empleado solo ve los suyos.</summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerHistorial([FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
    {
        var (items, total) = await _turnoCajaService.ObtenerHistorialAsync(EmpleadoId, EsAdmin, pagina, tamanoPagina);

        return Ok(new
        {
            data = items,
            pagina,
            tamanoPagina,
            total,
            totalPaginas = (int)Math.Ceiling((double)total / Math.Max(1, tamanoPagina))
        });
    }
}
