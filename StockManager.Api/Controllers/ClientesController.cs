using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Exceptions;

namespace StockManager.Api.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize(Roles = "Admin,Empleado")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clienteService;

    public ClientesController(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    /// <summary>
    /// Busca clientes por nombre o número de identificación. Sin el parámetro "activo" trae
    /// todos (para el panel de administración); con activo=true/false filtra por estado.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> BuscarClientes([FromQuery] string? busqueda, [FromQuery] bool? activo)
    {
        var clientes = await _clienteService.BuscarClientesAsync(busqueda, activo);
        return Ok(clientes);
    }

    /// <summary>
    /// Obtiene un cliente por su ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerClientePorId(int id)
    {
        var cliente = await _clienteService.ObtenerClientePorIdAsync(id);
        if (cliente == null)
            return NotFound(new { message = "Cliente no encontrado" });

        return Ok(cliente);
    }

    /// <summary>
    /// Registra un cliente directamente desde el panel. Solo los clientes registrados
    /// (así o autoregistrados desde la PWA) pueden ser parte de una Cuenta Abierta (fiado).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearClienteRequest request)
    {
        try
        {
            var resultado = await _clienteService.CrearClienteAsync(request);
            return CreatedAtAction(nameof(ObtenerClientePorId), new { id = resultado.Cliente.Id }, resultado);
        }
        catch (UsuarioDuplicadoPorIdentificacionException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UsuarioDuplicadoPorEmailException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error al crear el cliente" });
        }
    }

    /// <summary>
    /// Actualiza los datos de contacto de un cliente.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarClienteRequest request)
    {
        try
        {
            var cliente = await _clienteService.ActualizarClienteAsync(id, request);
            return Ok(cliente);
        }
        catch (ClienteNoEncontradoException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UsuarioDuplicadoPorEmailException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error al actualizar el cliente" });
        }
    }

    /// <summary>
    /// Desactiva un cliente (no se borran, para conservar el historial de ventas/pedidos).
    /// Se rechaza si el cliente todavía tiene pedidos en un estado activo.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Desactivar(int id)
    {
        try
        {
            var cliente = await _clienteService.DesactivarClienteAsync(id);
            return Ok(cliente);
        }
        catch (ClienteNoEncontradoException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ClienteConPedidosActivosException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error al desactivar el cliente" });
        }
    }

    /// <summary>
    /// Reactiva un cliente previamente desactivado.
    /// </summary>
    [HttpPost("{id}/activar")]
    public async Task<IActionResult> Activar(int id)
    {
        try
        {
            var cliente = await _clienteService.ActivarClienteAsync(id);
            return Ok(cliente);
        }
        catch (ClienteNoEncontradoException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error al activar el cliente" });
        }
    }
}
