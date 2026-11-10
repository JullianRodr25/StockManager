using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Constants;
using StockManager.Domain.Common;

namespace StockManager.Api.Controllers;

/// <summary>
/// Retiros parciales de efectivo de la caja (sacar lo vendido y dejar la base). Las reglas
/// viven en RetiroCaja / RetiroCajaService; el middleware global traduce los errores de dominio.
/// </summary>
[ApiController]
[Route("api/caja")]
[Authorize(Roles = Roles.AdminYCaja)]
public class RetirosCajaController : ControllerBase
{
    private readonly IRetiroCajaService _retiroCajaService;

    public RetirosCajaController(IRetiroCajaService retiroCajaService)
    {
        _retiroCajaService = retiroCajaService;
    }

    private int EmpleadoId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool EsAdmin => User.IsInRole(Roles.Admin);

    /// <summary>Registra un retiro en el turno abierto del usuario.</summary>
    [HttpPost("retiros")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarRetiroRequest request) =>
        Ok(await _retiroCajaService.RegistrarAsync(EmpleadoId, EsAdmin, request));

    /// <summary>Anula un retiro (solo Admin y solo con el turno abierto).</summary>
    [HttpPost("retiros/{id:int}/anular")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Anular(int id) =>
        Ok(await _retiroCajaService.AnularAsync(id, EmpleadoId));

    /// <summary>Retiros de un turno.</summary>
    [HttpGet("{turnoId:int}/retiros")]
    public async Task<IActionResult> ObtenerPorTurno(int turnoId) =>
        Ok(await _retiroCajaService.ObtenerPorTurnoAsync(turnoId, EmpleadoId, EsAdmin));

    /// <summary>Reporte de retiros por rango de fechas (por defecto, el mes en curso).</summary>
    [HttpGet("retiros")]
    public async Task<IActionResult> Reporte(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, [FromQuery] int? empleadoId)
    {
        var hoy = DateOnly.FromDateTime(HoraColombia.Hoy());
        var fin = hasta ?? hoy;
        var inicio = desde ?? new DateOnly(fin.Year, fin.Month, 1);
        return Ok(await _retiroCajaService.ReporteAsync(inicio, fin, empleadoId));
    }
}
