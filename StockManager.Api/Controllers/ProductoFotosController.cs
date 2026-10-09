using Microsoft.AspNetCore.Mvc;
using StockManager.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Infrastructure.Data;

namespace StockManager.Api.Controllers;

/// <summary>
/// Gestiona la galería de fotos de un producto (subir/eliminar), respaldada por Azure Blob
/// Storage. A propósito separado de ProductosController/IProductoService: este controlador
/// depende directamente de IBlobStorageService, que todavía NO está registrado en el
/// contenedor de DI (Program.cs) porque el recurso de Azure Storage Account aún no existe
/// (ver IBlobStorageService.cs/AzureBlobStorageService.cs). Si ProductoService dependiera de
/// IBlobStorageService, toda la API fallaría al arrancar (ValidateOnBuild en Development
/// revisa el grafo de TODO servicio registrado con AddScoped/AddTransient/AddSingleton) —
/// un Controller en cambio se activa por request, así que mientras nadie llame a estos dos
/// endpoints, el resto de la aplicación sigue funcionando con normalidad.
///
/// Para activar esta funcionalidad una vez exista el Storage Account: registrar
/// BlobStorageOptions + IBlobStorageService en Program.cs (ver el bloque ya escrito y
/// comentado ahí) y agregar la cadena de conexión en la configuración de Azure App Service.
/// </summary>
[ApiController]
[Route("api/productos/{productoId}/fotos")]
[Authorize(Roles = Roles.PersonalConInventario)]
public class ProductoFotosController : ControllerBase
{
    private static readonly string[] TiposPermitidos = { "image/jpeg", "image/png", "image/webp" };
    private const long TamanoMaximoBytes = 5 * 1024 * 1024; // 5 MB

    private readonly AppDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;

    public ProductoFotosController(AppDbContext dbContext, IBlobStorageService blobStorageService)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
    }

    /// <summary>
    /// Agrega una foto a la galería del producto. Máximo Producto.MaxFotos por producto.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AgregarFoto(int productoId, [FromForm] IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
            return BadRequest(new { message = "El archivo no puede estar vacío." });

        if (!TiposPermitidos.Contains(archivo.ContentType))
            return BadRequest(new { message = "Solo se permiten imágenes JPG, PNG o WEBP." });

        if (archivo.Length > TamanoMaximoBytes)
            return BadRequest(new { message = "La imagen no puede superar los 5 MB." });

        var producto = await _dbContext.Productos.FindAsync(productoId);
        if (producto == null)
            return NotFound(new { message = $"No se encontró el producto con ID {productoId}." });

        var totalFotosActuales = await _dbContext.ProductoFotos.CountAsync(f => f.ProductoId == productoId);
        if (totalFotosActuales >= Producto.MaxFotos)
            return BadRequest(new { message = $"El producto ya tiene el máximo de {Producto.MaxFotos} fotos permitidas." });

        var extension = Path.GetExtension(archivo.FileName);
        var nombreBlob = $"productos/{productoId}/{Guid.NewGuid()}{extension}";

        string url;
        using (var stream = archivo.OpenReadStream())
        {
            url = await _blobStorageService.SubirArchivoAsync(stream, nombreBlob, archivo.ContentType);
        }

        var siguienteOrden = totalFotosActuales == 0
            ? 0
            : await _dbContext.ProductoFotos
                .Where(f => f.ProductoId == productoId)
                .MaxAsync(f => f.Orden) + 1;

        var foto = ProductoFoto.Crear(productoId, url, siguienteOrden);
        _dbContext.ProductoFotos.Add(foto);
        await _dbContext.SaveChangesAsync();

        return Ok(new ProductoFotoResponse(foto.Id, foto.Url, foto.Orden));
    }

    /// <summary>
    /// Elimina una foto de la galería del producto.
    /// </summary>
    [HttpDelete("{fotoId}")]
    public async Task<IActionResult> EliminarFoto(int productoId, int fotoId)
    {
        var foto = await _dbContext.ProductoFotos
            .FirstOrDefaultAsync(f => f.Id == fotoId && f.ProductoId == productoId);

        if (foto == null)
            return NotFound(new { message = $"No se encontró la foto {fotoId} para el producto {productoId}." });

        // Se borra primero el registro (lo que el usuario percibe como "la operación") y el
        // blob físico queda como mejor esfuerzo: igual criterio que el resto de limpiezas de
        // blobs en este backend (ver BorrarBlobAnteriorMejorEsfuerzo en el feature de foto de
        // perfil) — un fallo al borrar el archivo en Azure no debe revertir algo que la UI ya
        // confirmó como eliminado.
        _dbContext.ProductoFotos.Remove(foto);
        await _dbContext.SaveChangesAsync();

        try
        {
            await _blobStorageService.EliminarArchivoAsync(foto.Url);
        }
        catch
        {
            // Best-effort — ver comentario arriba.
        }

        return Ok(new { message = "Foto eliminada correctamente." });
    }
}
