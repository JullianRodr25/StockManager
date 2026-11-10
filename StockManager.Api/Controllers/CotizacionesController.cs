using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Constants;

namespace StockManager.Api.Controllers;

/// <summary>
/// Cotizaciones: ofertas con plazo, editables, que se convierten en venta. Visibles y operables
/// por Admin y Caja. Las reglas de negocio viven en el dominio y en los servicios; el middleware
/// global traduce los errores (404 no encontrada, 409 no modificable, 400 datos inválidos).
/// </summary>
[ApiController]
[Route("api/cotizaciones")]
[Authorize(Roles = Roles.AdminYCaja)]
public class CotizacionesController : ControllerBase
{
    private const int MaximoTamanoPagina = 100;

    private readonly ICotizacionService _cotizaciones;
    private readonly IVentaService _ventas;

    public CotizacionesController(ICotizacionService cotizaciones, IVentaService ventas)
    {
        _cotizaciones = cotizaciones;
        _ventas = ventas;
    }

    private int EmpleadoId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool EsAdmin => User.IsInRole(Roles.Admin);

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? estado = null,
        [FromQuery] string? busqueda = null,
        [FromQuery] DateOnly? desde = null,
        [FromQuery] DateOnly? hasta = null)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, MaximoTamanoPagina);

        var (items, total) = await _cotizaciones.ListarAsync(pagina, tamanoPagina, estado, busqueda, desde, hasta);
        return Ok(new { data = items, pagina, tamanoPagina, total, totalPaginas = (int)Math.Ceiling((double)total / tamanoPagina) });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id) => Ok(await _cotizaciones.ObtenerAsync(id));

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarCotizacionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        return Ok(await _cotizaciones.CrearAsync(request, EmpleadoId, EsAdmin));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarCotizacionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        return Ok(await _cotizaciones.ActualizarAsync(id, request, EsAdmin));
    }

    [HttpPost("{id:int}/anular")]
    public async Task<IActionResult> Anular(int id) => Ok(await _cotizaciones.AnularAsync(id));

    /// <summary>Convierte la cotización en una venta cobrada (precios cotizados, stock descontado).</summary>
    [HttpPost("{id:int}/convertir")]
    public async Task<IActionResult> Convertir(int id, [FromBody] ConvertirCotizacionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        return Ok(await _ventas.RegistrarVentaDesdeCotizacionAsync(id, request, EmpleadoId));
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> Pdf(int id)
    {
        var pdf = await _cotizaciones.GenerarPdfAsync(id);
        return File(pdf, "application/pdf", $"cotizacion-{id:D5}.pdf");
    }
}
