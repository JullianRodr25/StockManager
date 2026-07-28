using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StockManager.Application.Services;
using StockManager.Application.DTOs;

namespace StockManager.Api.Controllers;

[ApiController]
[Route("api/productos")]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _productoService;

    public ProductosController(IProductoService productoService)
    {
        _productoService = productoService;
    }

    /// <summary>
    /// Obtiene una lista paginada de productos.
    /// Requiere autenticación con rol Admin o Empleado.
    /// </summary>
    /// <param name="pagina">Número de página (1-based), default 1</param>
    /// <param name="tamanoPagina">Cantidad de items por página, default 50</param>
    /// <param name="categoriaId">ID de categoría opcional para filtrar</param>
    [HttpGet]
    [Authorize(Roles = "Admin,Empleado")]
    public async Task<IActionResult> ObtenerProductos(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 50,
        [FromQuery] int? categoriaId = null)
    {
        var (items, total) = await _productoService.ObtenerProductosPaginadoAsync(pagina, tamanoPagina, categoriaId);

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
    /// Obtiene un producto por su ID.
    /// Requiere autenticación con rol Admin o Empleado.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Empleado")]
    public async Task<IActionResult> ObtenerProductoPorId(int id)
    {
        var producto = await _productoService.ObtenerProductoPorIdAsync(id);
        if (producto == null)
            return NotFound(new { message = "Producto no encontrado" });

        return Ok(producto);
    }

    /// <summary>
    /// Busca un producto por su código de barras (exacto).
    /// Retorna 404 si no existe.
    /// Requiere autenticación con rol Admin o Empleado.
    /// </summary>
    [HttpGet("buscar-codigo-barras/{codigo}")]
    [Authorize(Roles = "Admin,Empleado")]
    public async Task<IActionResult> BuscarPorCodigoBarras(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return BadRequest(new { message = "El código de barras no puede estar vacío" });

        var producto = await _productoService.ObtenerProductoPorCodigoBarrasAsync(codigo.Trim());
        if (producto == null)
            return NotFound(new { message = "Producto con ese código de barras no encontrado" });

        return Ok(producto);
    }

    /// <summary>
    /// Crea un nuevo producto.
    /// Si CodigoBarras viene vacío, el sistema genera uno automáticamente.
    /// Requiere autenticación con rol Admin.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CrearProducto([FromBody] CrearProductoRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var productoResponse = await _productoService.CrearProductoAsync(request);
            return CreatedAtAction(nameof(ObtenerProductoPorId), new { id = productoResponse.Id }, productoResponse);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // TODO: loguear ex con ILogger cuando se agregue logging
            return StatusCode(500, new { message = "Error al crear el producto" });
        }
    }

    /// <summary>
    /// Importa productos masivamente desde un archivo Excel.
    /// Columnas esperadas: Nombre, Categoria, Precio, StockInicial, StockMinimo, CodigoBarras (opcional).
    /// Continúa con las siguientes filas incluso si algunas fallan.
    /// Requiere autenticación con rol Admin.
    /// </summary>
    [HttpPost("importar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ImportarProductos([FromForm] IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
            return BadRequest(new { message = "El archivo no puede estar vacío" });

        if (!archivo.FileName.EndsWith(".xlsx") && !archivo.FileName.EndsWith(".csv"))
            return BadRequest(new { message = "El archivo debe ser .xlsx o .csv" });

        try
        {
            using (var stream = archivo.OpenReadStream())
            {
                var resultado = await _productoService.ImportarProductosDesdeExcelAsync(stream);
                return Ok(resultado);
            }
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // TODO: loguear ex con ILogger cuando se agregue logging
            return StatusCode(500, new { message = "Error al importar productos" });
        }
    }

    /// <summary>
    /// Obtiene los productos con etiquetas pendientes de imprimir.
    /// Filtra por EsCodigoGenerado = true AND FechaImpresionEtiqueta IS NULL.
    /// Requiere autenticación con rol Admin.
    /// </summary>
    [HttpGet("etiquetas-pendientes")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ObtenerEtiquetasPendientes()
    {
        var etiquetas = await _productoService.ObtenerEtiquetasPendientesAsync();
        return Ok(new
        {
            total = etiquetas.Count,
            etiquetas
        });
    }

    /// <summary>
    /// Genera imágenes de códigos de barras en base64 para una lista de productos.
    /// Marca FechaImpresionEtiqueta = ahora para cada uno.
    /// Requiere autenticación con rol Admin.
    /// </summary>
    [HttpPost("generar-etiquetas")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GenerarEtiquetas([FromBody] List<int> productosIds)
    {
        if (productosIds == null || productosIds.Count == 0)
            return BadRequest(new { message = "Debe proporcionar al menos un ID de producto" });

        try
        {
            var etiquetas = await _productoService.GenerarEtiquetasAsync(productosIds);
            return Ok(new
            {
                total = etiquetas.Count,
                etiquetas
            });
        }
        catch (Exception ex)
        {
            // TEMPORAL: Loguear excepción completa a consola para debugging
            Console.WriteLine($"ERROR en GenerarEtiquetas: {ex.GetType().FullName}");
            Console.WriteLine($"Mensaje: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"\nINNER EXCEPTION: {ex.InnerException.GetType().FullName}");
                Console.WriteLine($"Mensaje: {ex.InnerException.Message}");
                Console.WriteLine($"StackTrace: {ex.InnerException.StackTrace}");
            }
            return StatusCode(500, new { message = "Error al generar etiquetas" });
        }
    }
}
