using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;

namespace StockManager.Api.Controllers;

/// <summary>
/// Autoservicio del Cliente autenticado desde la PWA: ver/editar sus propios datos de
/// contacto, cambiar su propia contraseña y gestionar su foto de perfil. Deliberadamente
/// separado de ClientesController (que es [Authorize(Roles = "Admin,Empleado")] y opera sobre
/// cualquier cliente por ID): acá el ID sale siempre del token, nunca del request, así que un
/// cliente no puede leer ni tocar la cuenta de otro cambiando un parámetro. Ambos
/// controladores reutilizan el mismo IClienteService como única fuente de verdad para las
/// reglas de negocio de contacto/facturación.
///
/// Los dos endpoints de foto (AgregarFoto/EliminarFoto) son la excepción: igual que
/// ProductoFotosController, acceden directo a AppDbContext + IBlobStorageService en vez de
/// pasar por IClienteService, para no acoplar el servicio principal de Cliente (que si
/// dependiera de Azure Blob Storage, repetiría el riesgo de ValidateOnBuild que ya evitamos
/// una vez con las fotos de producto) a una dependencia que ahora mismo SÍ está registrada,
/// pero que conceptualmente es una preocupación de infraestructura aparte de las reglas de
/// negocio de "Cliente".
/// </summary>
[ApiController]
[Route("api/mi-cuenta")]
[Authorize(Roles = "Cliente")]
public class MiCuentaController : ControllerBase
{
    private static readonly string[] TiposFotoPermitidos = { "image/jpeg", "image/png", "image/webp" };
    private const long TamanoMaximoFotoBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IClienteService _clienteService;
    private readonly AppDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;

    public MiCuentaController(
        IClienteService clienteService,
        AppDbContext dbContext,
        IBlobStorageService blobStorageService)
    {
        _clienteService = clienteService;
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
    }

    private int ClienteIdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Obtiene los datos del cliente autenticado (pantalla "Mi perfil" de la PWA).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerMiCuenta()
    {
        var cliente = await _clienteService.ObtenerClientePorIdAsync(ClienteIdActual);
        if (cliente == null)
            return NotFound(new { message = "Cliente no encontrado" });

        return Ok(cliente);
    }

    /// <summary>
    /// Actualiza los datos de contacto (nombre, email, teléfono, dirección) del propio
    /// cliente autenticado. Misma validación y reglas que la edición desde el panel
    /// (ClientesController.Actualizar), aplicadas siempre sobre el cliente del token.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> ActualizarMiCuenta([FromBody] ActualizarClienteRequest request)
    {
        try
        {
            var cliente = await _clienteService.ActualizarClienteAsync(ClienteIdActual, request);
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
            return StatusCode(500, new { message = "Error al actualizar tus datos" });
        }
    }

    /// <summary>
    /// Cambia la contraseña del propio cliente autenticado, exigiendo la contraseña actual.
    /// </summary>
    [HttpPost("cambiar-password")]
    public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordPropioRequest request)
    {
        try
        {
            await _clienteService.CambiarPasswordPropioAsync(ClienteIdActual, request);
            return Ok(new { message = "Contraseña actualizada correctamente" });
        }
        catch (ClienteNoEncontradoException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ContrasenaActualIncorrectaException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error al cambiar la contraseña" });
        }
    }

    /// <summary>
    /// Sube (o reemplaza) la foto de perfil del cliente autenticado. Si ya tenía una, la
    /// anterior se borra de Azure Blob Storage como mejor esfuerzo después de guardar la
    /// nueva (igual criterio que EliminarFoto más abajo: un fallo al limpiar el blob viejo no
    /// debe revertir la foto nueva, que es lo que el usuario ya ve confirmado en pantalla).
    /// </summary>
    [HttpPost("foto")]
    public async Task<IActionResult> AgregarFoto([FromForm] IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
            return BadRequest(new { message = "El archivo no puede estar vacío." });

        if (!TiposFotoPermitidos.Contains(archivo.ContentType))
            return BadRequest(new { message = "Solo se permiten imágenes JPG, PNG o WEBP." });

        if (archivo.Length > TamanoMaximoFotoBytes)
            return BadRequest(new { message = "La imagen no puede superar los 5 MB." });

        var cliente = await _dbContext.Clientes.FirstOrDefaultAsync(c => c.Id == ClienteIdActual);
        if (cliente == null)
            return NotFound(new { message = "Cliente no encontrado" });

        var urlAnterior = cliente.FotoUrl;

        var extension = Path.GetExtension(archivo.FileName);
        var nombreBlob = $"clientes/{ClienteIdActual}/{Guid.NewGuid()}{extension}";

        string url;
        using (var stream = archivo.OpenReadStream())
        {
            url = await _blobStorageService.SubirArchivoAsync(stream, nombreBlob, archivo.ContentType);
        }

        cliente.ActualizarFoto(url);
        await _dbContext.SaveChangesAsync();

        await BorrarBlobAnteriorMejorEsfuerzo(urlAnterior);

        var clienteActualizado = await _clienteService.ObtenerClientePorIdAsync(ClienteIdActual);
        return Ok(clienteActualizado);
    }

    /// <summary>Quita la foto de perfil del cliente autenticado.</summary>
    [HttpDelete("foto")]
    public async Task<IActionResult> EliminarFoto()
    {
        var cliente = await _dbContext.Clientes.FirstOrDefaultAsync(c => c.Id == ClienteIdActual);
        if (cliente == null)
            return NotFound(new { message = "Cliente no encontrado" });

        var urlAnterior = cliente.FotoUrl;

        cliente.EliminarFoto();
        await _dbContext.SaveChangesAsync();

        await BorrarBlobAnteriorMejorEsfuerzo(urlAnterior);

        var clienteActualizado = await _clienteService.ObtenerClientePorIdAsync(ClienteIdActual);
        return Ok(clienteActualizado);
    }

    /// <summary>
    /// Borra un blob de foto de perfil previo sin que un fallo (o que simplemente no hubiera
    /// ninguno) interrumpa la respuesta — el registro en base de datos ya quedó consistente
    /// antes de llamar esto, que es "la operación" tal como la percibe el usuario.
    /// </summary>
    private async Task BorrarBlobAnteriorMejorEsfuerzo(string? urlAnterior)
    {
        if (string.IsNullOrWhiteSpace(urlAnterior))
            return;

        try
        {
            await _blobStorageService.EliminarArchivoAsync(urlAnterior);
        }
        catch
        {
            // Best-effort — ver comentario de la función.
        }
    }
}
