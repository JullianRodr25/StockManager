using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Exceptions;

namespace StockManager.Api.Controllers;

[ApiController]
[Route("api/pedidos")]
public class PedidosController : ControllerBase
{
    private readonly IPedidoService _pedidoService;

    public PedidosController(IPedidoService pedidoService)
    {
        _pedidoService = pedidoService;
    }

    /// <summary>
    /// Crea un pedido a domicilio desde la PWA. El cliente sale del token, no del body.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> CrearPedido([FromBody] CrearPedidoRequest request)
    {
        try
        {
            var clienteId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var pedido = await _pedidoService.CrearPedidoAsync(request, clienteId);
            return Ok(pedido);
        }
        catch (StockInsuficienteException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ProductoInactivoException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ConcurrencyException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
        {
            return StatusCode(500, new { message = "Error al crear el pedido" });
        }
    }

    /// <summary>
    /// Lista los pedidos propios del cliente autenticado (para la PWA).
    /// </summary>
    [HttpGet("mios")]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> ObtenerMisPedidos(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? estado = null)
    {
        var clienteId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (items, total) = await _pedidoService.ObtenerPedidosPaginadoAsync(pagina, tamanoPagina, estado, clienteId);

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
    /// Lista todos los pedidos (para la web de administración).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Empleado")]
    public async Task<IActionResult> ObtenerPedidos(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? estado = null)
    {
        var (items, total) = await _pedidoService.ObtenerPedidosPaginadoAsync(pagina, tamanoPagina, estado, clienteId: null);

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
    /// Obtiene un pedido por su ID. Un cliente solo puede ver los suyos.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> ObtenerPedidoPorId(int id)
    {
        var pedido = await _pedidoService.ObtenerPedidoPorIdAsync(id);
        if (pedido == null)
            return NotFound(new { message = "Pedido no encontrado" });

        var esCliente = User.IsInRole("Cliente");
        if (esCliente)
        {
            var clienteId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (pedido.ClienteId != clienteId)
                return NotFound(new { message = "Pedido no encontrado" });
        }

        return Ok(pedido);
    }

    /// <summary>
    /// El admin confirma que el pedido se va a atender.
    /// </summary>
    [HttpPut("{id}/confirmar")]
    [Authorize(Roles = "Admin,Empleado")]
    public async Task<IActionResult> Confirmar(int id) =>
        await EjecutarTransicionAsync(() => _pedidoService.ConfirmarAsync(id), "confirmar el pedido");

    /// <summary>
    /// El pedido pasa a alistarse en bodega.
    /// </summary>
    [HttpPut("{id}/preparacion")]
    [Authorize(Roles = "Admin,Empleado")]
    public async Task<IActionResult> IniciarPreparacion(int id) =>
        await EjecutarTransicionAsync(() => _pedidoService.IniciarPreparacionAsync(id), "iniciar la preparación del pedido");

    /// <summary>
    /// El pedido sale a domicilio.
    /// </summary>
    [HttpPut("{id}/camino")]
    [Authorize(Roles = "Admin,Empleado")]
    public async Task<IActionResult> EnviarACamino(int id) =>
        await EjecutarTransicionAsync(() => _pedidoService.EnviarACaminoAsync(id), "enviar el pedido a camino");

    /// <summary>
    /// Marca el pedido como entregado: genera la Venta y la Factura equivalentes.
    /// </summary>
    [HttpPut("{id}/entregado")]
    [Authorize(Roles = "Admin,Empleado")]
    public async Task<IActionResult> MarcarEntregado(int id, [FromBody] MarcarEntregadoRequest request)
    {
        try
        {
            var empleadoId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var pedido = await _pedidoService.MarcarEntregadoAsync(id, request.MetodoPago, empleadoId);
            return Ok(pedido);
        }
        catch (PedidoNoEncontradoException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (PedidoEstadoInvalidoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (OperacionInvalidaPedidoException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
        {
            return StatusCode(500, new { message = "Error al marcar el pedido como entregado" });
        }
    }

    /// <summary>
    /// Cancela el pedido y repone el stock de sus líneas disponibles.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Empleado")]
    public async Task<IActionResult> Cancelar(int id) =>
        await EjecutarTransicionAsync(() => _pedidoService.CancelarAsync(id), "cancelar el pedido");

    /// <summary>
    /// Envuelve las transiciones de estado simples (confirmar/preparación/camino/cancelar),
    /// que comparten el mismo mapeo de excepciones de dominio a códigos HTTP.
    /// </summary>
    private async Task<IActionResult> EjecutarTransicionAsync(Func<Task<PedidoResponse>> operacion, string descripcionError)
    {
        try
        {
            var pedido = await operacion();
            return Ok(pedido);
        }
        catch (PedidoNoEncontradoException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (PedidoEstadoInvalidoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ConcurrencyException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
        {
            return StatusCode(500, new { message = $"Error al {descripcionError}" });
        }
    }
}
