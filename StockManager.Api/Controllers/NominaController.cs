using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Constants;

namespace StockManager.Api.Controllers;

/// <summary>
/// Nómina del negocio: trabajadores, préstamos y quincenas. Solo para administradores, porque
/// maneja sueldos. Las reglas viven en las entidades y en los servicios; los errores de dominio
/// los traduce el ManejadorExcepcionesMiddleware, por eso aquí no hay try/catch.
/// </summary>
[ApiController]
[Route("api/nomina")]
[Authorize(Roles = Roles.Admin)]
public class NominaController : ControllerBase
{
    private readonly ITrabajadorService _trabajadores;
    private readonly IPrestamoService _prestamos;
    private readonly IPeriodoNominaService _periodos;
    private readonly IParametrosNominaService _parametros;
    private readonly INominaResumenService _resumen;

    public NominaController(
        ITrabajadorService trabajadores,
        IPrestamoService prestamos,
        IPeriodoNominaService periodos,
        IParametrosNominaService parametros,
        INominaResumenService resumen)
    {
        _trabajadores = trabajadores;
        _prestamos = prestamos;
        _periodos = periodos;
        _parametros = parametros;
        _resumen = resumen;
    }

    private int EmpleadoId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ---------- Panel ----------

    /// <summary>Cifras del panel de nómina y alertas (contratos, prima, quincena sin abrir).</summary>
    [HttpGet("resumen")]
    public async Task<IActionResult> ObtenerResumen() => Ok(await _resumen.ObtenerAsync());

    // ---------- Trabajadores ----------

    [HttpGet("trabajadores")]
    public async Task<IActionResult> ListarTrabajadores([FromQuery] bool incluirInactivos = false) =>
        Ok(await _trabajadores.ListarAsync(incluirInactivos));

    [HttpGet("trabajadores/{id:int}")]
    public async Task<IActionResult> ObtenerTrabajador(int id) => Ok(await _trabajadores.ObtenerAsync(id));

    [HttpPost("trabajadores")]
    public async Task<IActionResult> CrearTrabajador([FromBody] GuardarTrabajadorRequest request)
    {
        var trabajador = await _trabajadores.CrearAsync(request);
        return CreatedAtAction(nameof(ObtenerTrabajador), new { id = trabajador.Id }, trabajador);
    }

    [HttpPut("trabajadores/{id:int}")]
    public async Task<IActionResult> ActualizarTrabajador(int id, [FromBody] GuardarTrabajadorRequest request) =>
        Ok(await _trabajadores.ActualizarAsync(id, request));

    [HttpPost("trabajadores/{id:int}/activar")]
    public async Task<IActionResult> ActivarTrabajador(int id) =>
        Ok(await _trabajadores.CambiarEstadoAsync(id, true));

    [HttpPost("trabajadores/{id:int}/desactivar")]
    public async Task<IActionResult> DesactivarTrabajador(int id) =>
        Ok(await _trabajadores.CambiarEstadoAsync(id, false));

    // ---------- Parámetros ----------

    [HttpGet("parametros")]
    public async Task<IActionResult> ObtenerParametros() => Ok(await _parametros.ObtenerAsync());

    [HttpPut("parametros")]
    public async Task<IActionResult> ActualizarParametros([FromBody] ParametrosNominaRequest request)
    {
        var parametros = await _parametros.ActualizarAsync(request);
        // Los parámetros cambian el cálculo: las quincenas en borrador se actualizan.
        await _periodos.RecalcularBorradoresAsync();
        return Ok(parametros);
    }

    // ---------- Préstamos ----------

    [HttpGet("prestamos")]
    public async Task<IActionResult> ListarPrestamos([FromQuery] string? estado, [FromQuery] int? trabajadorId) =>
        Ok(await _prestamos.ListarAsync(estado, trabajadorId));

    [HttpPost("prestamos")]
    public async Task<IActionResult> CrearPrestamo([FromBody] CrearPrestamoRequest request) =>
        Ok(await _prestamos.CrearAsync(request));

    [HttpPost("prestamos/{id:int}/cancelar")]
    public async Task<IActionResult> CancelarPrestamo(int id) => Ok(await _prestamos.CancelarAsync(id));

    // ---------- Quincenas ----------

    [HttpGet("periodos")]
    public async Task<IActionResult> ListarPeriodos() => Ok(await _periodos.ListarAsync());

    /// <summary>Período de la frecuencia (por defecto Quincenal) que contiene el día de hoy; 204 si aún no se abrió.</summary>
    [HttpGet("periodos/actual")]
    public async Task<IActionResult> ObtenerPeriodoActual([FromQuery] string frecuencia = "Quincenal")
    {
        var periodo = await _periodos.ObtenerActualAsync(frecuencia);
        return periodo is null ? NoContent() : Ok(periodo);
    }

    [HttpGet("periodos/{id:int}")]
    public async Task<IActionResult> ObtenerPeriodo(int id) => Ok(await _periodos.ObtenerAsync(id));

    [HttpPost("periodos")]
    public async Task<IActionResult> AbrirPeriodo([FromBody] AbrirPeriodoNominaRequest? request) =>
        Ok(await _periodos.AbrirAsync(request ?? new AbrirPeriodoNominaRequest(null, null)));

    [HttpPost("periodos/{id:int}/recalcular")]
    public async Task<IActionResult> RecalcularPeriodo(int id) => Ok(await _periodos.RecalcularAsync(id));

    [HttpPost("periodos/{id:int}/aprobar")]
    public async Task<IActionResult> AprobarPeriodo(int id) => Ok(await _periodos.AprobarAsync(id, EmpleadoId));

    /// <summary>Registra el pago de todos los trabajadores que aún no se han marcado como pagados. Los pagos en efectivo salen del turno de caja abierto de quien los registra.</summary>
    [HttpPost("periodos/{id:int}/pagar")]
    public async Task<IActionResult> PagarPeriodo(int id) => Ok(await _periodos.PagarTodasAsync(id, EmpleadoId));

    [HttpPost("periodos/{id:int}/liquidaciones/{liquidacionId:int}/pagar")]
    public async Task<IActionResult> PagarLiquidacion(int id, int liquidacionId) =>
        Ok(await _periodos.PagarLiquidacionAsync(id, liquidacionId, EmpleadoId));

    [HttpPost("periodos/{id:int}/cerrar")]
    public async Task<IActionResult> CerrarPeriodo(int id) => Ok(await _periodos.CerrarAsync(id));

    [HttpPost("periodos/{id:int}/novedades")]
    public async Task<IActionResult> AgregarNovedad(int id, [FromBody] AgregarNovedadRequest request) =>
        Ok(await _periodos.AgregarNovedadAsync(id, request));

    [HttpDelete("periodos/{id:int}/novedades/{novedadId:int}")]
    public async Task<IActionResult> EliminarNovedad(int id, int novedadId) =>
        Ok(await _periodos.EliminarNovedadAsync(id, novedadId));
}
