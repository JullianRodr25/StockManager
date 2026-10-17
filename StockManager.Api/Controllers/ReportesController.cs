using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.Services;
using StockManager.Domain.Common;

namespace StockManager.Api.Controllers;

/// <summary>Reportes administrativos: solo el Admin los ve (incluyen costos, utilidades y cartera).</summary>
[ApiController]
[Route("api/reportes")]
[Authorize(Roles = "Admin")]
public class ReportesController : ControllerBase
{
    private const int MaximoDiasRango = 366;
    private const int MaximoTop = 100;

    private readonly IReporteService _reportes;

    public ReportesController(IReporteService reportes)
    {
        _reportes = reportes;
    }

    // Sin fechas: el mes en curso (en hora de Colombia).
    private static (DateOnly Desde, DateOnly Hasta) ResolverRango(DateOnly? desde, DateOnly? hasta)
    {
        var hoy = DateOnly.FromDateTime(HoraColombia.Hoy());
        return (desde ?? new DateOnly(hoy.Year, hoy.Month, 1), hasta ?? hoy);
    }

    private IActionResult? ValidarRango(DateOnly desde, DateOnly hasta)
    {
        if (hasta < desde)
            return BadRequest(new { message = "La fecha final no puede ser anterior a la inicial." });
        if (hasta.DayNumber - desde.DayNumber > MaximoDiasRango)
            return BadRequest(new { message = $"El rango no puede superar {MaximoDiasRango} días." });
        return null;
    }

    [HttpGet("ventas")]
    public async Task<IActionResult> Ventas([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta)
    {
        var (d, h) = ResolverRango(desde, hasta);
        return ValidarRango(d, h) ?? Ok(await _reportes.VentasAsync(d, h));
    }

    [HttpGet("productos-vendidos")]
    public async Task<IActionResult> ProductosVendidos(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, [FromQuery] int top = 20)
    {
        var (d, h) = ResolverRango(desde, hasta);
        return ValidarRango(d, h) ?? Ok(await _reportes.ProductosVendidosAsync(d, h, Math.Clamp(top, 1, MaximoTop)));
    }

    [HttpGet("inventario")]
    public async Task<IActionResult> Inventario() => Ok(await _reportes.InventarioAsync());

    [HttpGet("cartera")]
    public async Task<IActionResult> Cartera() => Ok(await _reportes.CarteraAsync());
}
