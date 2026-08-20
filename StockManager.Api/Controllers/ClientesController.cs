using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.Services;

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
    /// Busca clientes activos por nombre o número de identificación.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> BuscarClientes([FromQuery] string? busqueda)
    {
        var clientes = await _clienteService.BuscarClientesAsync(busqueda);
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
}
