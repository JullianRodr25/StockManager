using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Constants;
using StockManager.Domain.Exceptions;

namespace StockManager.Api.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize(Roles = "Admin,Empleado")]
public class ClientesController : ControllerBase
{
    private const int MaxBytesArchivoExcel = 5 * 1024 * 1024;

    private readonly IClienteService _clienteService;
    private readonly IClienteExcelImportador _clienteExcelImportador;

    public ClientesController(IClienteService clienteService, IClienteExcelImportador clienteExcelImportador)
    {
        _clienteService = clienteService;
        _clienteExcelImportador = clienteExcelImportador;
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
    /// Importa clientes desde el Excel de terceros del programa contable anterior (migración).
    /// Con <c>soloValidar=true</c> devuelve la vista previa sin guardar nada; con <c>false</c> crea los
    /// clientes, pero solo si TODAS las filas son válidas (todo o nada). Los clientes que ya existen
    /// (misma identificación) no se modifican.
    /// Requiere autenticación con rol Admin.
    /// </summary>
    [HttpPost("importar")]
    [Authorize(Roles = Roles.Admin)]
    [RequestSizeLimit(MaxBytesArchivoExcel)]
    public async Task<IActionResult> ImportarClientes([FromForm] IFormFile archivo, [FromQuery] bool soloValidar = false)
    {
        if (archivo == null || archivo.Length == 0)
            return BadRequest(new { message = "El archivo no puede estar vacío" });

        if (!archivo.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "El archivo debe ser .xlsx" });

        if (archivo.Length > MaxBytesArchivoExcel)
            return BadRequest(new { message = "El archivo supera el tamaño máximo permitido (5 MB)" });

        try
        {
            using var stream = archivo.OpenReadStream();
            var resultado = await _clienteExcelImportador.ProcesarAsync(stream, aplicarCambios: !soloValidar);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
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
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
        {
            return StatusCode(500, new { message = "Error al actualizar el cliente" });
        }
    }

    /// <summary>
    /// Actualiza los datos fiscales de un cliente (sección "Datos para factura electrónica"),
    /// separados de los datos de contacto que maneja Actualizar().
    /// </summary>
    [HttpPut("{id}/datos-facturacion")]
    public async Task<IActionResult> ActualizarDatosFacturacion(int id, [FromBody] ActualizarDatosFacturacionRequest request)
    {
        try
        {
            var cliente = await _clienteService.ActualizarDatosFacturacionElectronicaAsync(id, request);
            return Ok(cliente);
        }
        catch (ClienteNoEncontradoException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
        {
            return StatusCode(500, new { message = "Error al actualizar los datos de facturación" });
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
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
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
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
        {
            return StatusCode(500, new { message = "Error al activar el cliente" });
        }
    }
}
