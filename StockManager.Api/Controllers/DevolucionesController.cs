using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Constants;

namespace StockManager.Api.Controllers;

/// <summary>
/// Devoluciones y cambios de ventas ya pagadas. Los errores de negocio (venta inexistente, no
/// pagada, stock insuficiente en el cambio, sin caja abierta) los traduce ManejadorExcepcionesMiddleware.
/// </summary>
[ApiController]
[Authorize(Roles = Roles.AdminYCaja)]
public class DevolucionesController : ControllerBase
{
    private readonly IDevolucionService _devolucionService;

    public DevolucionesController(IDevolucionService devolucionService)
    {
        _devolucionService = devolucionService;
    }

    /// <summary>
    /// Registra una devolución, con o sin cambio, sobre una venta pagada.
    /// </summary>
    /// <response code="200">Devolución registrada</response>
    /// <response code="400">Cantidades inválidas, stock insuficiente en el cambio, producto inactivo</response>
    /// <response code="404">La venta no existe</response>
    /// <response code="409">La venta no está pagada o no hay caja abierta para un movimiento en efectivo</response>
    [HttpPost("api/ventas/{ventaId:int}/devoluciones")]
    public async Task<ActionResult<DevolucionResponse>> Registrar(int ventaId, [FromBody] RegistrarDevolucionRequest request)
    {
        var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _devolucionService.RegistrarAsync(ventaId, request, empleadoId));
    }

    /// <summary>
    /// Devoluciones ya registradas de una venta, de la más reciente a la más antigua.
    /// </summary>
    [HttpGet("api/ventas/{ventaId:int}/devoluciones")]
    public async Task<ActionResult<List<DevolucionResponse>>> ObtenerPorVenta(int ventaId)
    {
        return Ok(await _devolucionService.ObtenerPorVentaAsync(ventaId));
    }
}
