using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StockManager.Application.Services;
using StockManager.Application.DTOs;
using StockManager.Application.Excel;
using StockManager.Domain.Constants;

namespace StockManager.Api.Controllers;

[ApiController]
[Route("api/productos")]
public class ProductosController : ControllerBase
{
    private const int MaxBytesArchivoExcel = 5 * 1024 * 1024;

    private readonly IProductoService _productoService;
    private readonly IProductoExcelService _productoExcelService;
    private readonly IProductoExcelImportador _productoExcelImportador;
    private readonly ILogger<ProductosController> _logger;

    public ProductosController(
        IProductoService productoService,
        IProductoExcelService productoExcelService,
        IProductoExcelImportador productoExcelImportador,
        ILogger<ProductosController> logger)
    {
        _productoService = productoService;
        _productoExcelService = productoExcelService;
        _productoExcelImportador = productoExcelImportador;
        _logger = logger;
    }

    /// <summary>
    /// Al rol de solo consulta no se le entregan datos comerciales internos (costo y proveedor).
    /// Se quitan aquí, en el servidor, y no solo en la pantalla: ocultarlos en la web no impediría
    /// leerlos llamando a la API directamente.
    /// </summary>
    private void OcultarDatosInternosSiCorresponde(ProductoResponse producto)
    {
        if (!User.IsInRole(Roles.ConsultaInventario))
            return;

        producto.Costo = 0;
        producto.ProveedorId = null;
    }

    /// <summary>
    /// Obtiene una lista paginada de productos.
    /// Requiere autenticación con rol Admin o Empleado.
    /// </summary>
    /// <param name="pagina">Número de página (1-based), default 1</param>
    /// <param name="tamanoPagina">Cantidad de items por página, default 50</param>
    /// <param name="categoriaId">ID de categoría opcional para filtrar</param>
    /// <param name="busqueda">Texto opcional para buscar por nombre en todo el inventario</param>
    [HttpGet]
    [Authorize(Roles = Roles.LecturaInventario)]
    public async Task<IActionResult> ObtenerProductos(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 50,
        [FromQuery] int? categoriaId = null,
        [FromQuery] string? busqueda = null)
    {
        var (items, total) = await _productoService.ObtenerProductosPaginadoAsync(pagina, tamanoPagina, categoriaId, busqueda);
        items.ForEach(OcultarDatosInternosSiCorresponde);

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
    /// Productos activos con stock igual o por debajo del mínimo (agotados primero), para el
    /// resumen de alertas del inventario. Debe declararse antes de "{id}" para que la ruta
    /// literal no se confunda con un id.
    /// </summary>
    [HttpGet("alertas-stock")]
    [Authorize(Roles = Roles.LecturaInventario)]
    public async Task<IActionResult> ObtenerAlertasStock()
    {
        var alertas = await _productoService.ObtenerAlertasStockAsync();
        alertas.ForEach(OcultarDatosInternosSiCorresponde);
        return Ok(alertas);
    }

    /// <summary>
    /// Obtiene un producto por su ID.
    /// Requiere autenticación con rol Admin o Empleado.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = Roles.LecturaInventario)]
    public async Task<IActionResult> ObtenerProductoPorId(int id)
    {
        var producto = await _productoService.ObtenerProductoPorIdAsync(id);
        if (producto == null)
            return NotFound(new { message = "Producto no encontrado" });

        OcultarDatosInternosSiCorresponde(producto);
        return Ok(producto);
    }

    /// <summary>
    /// Busca un producto por su código de barras (exacto).
    /// Retorna 404 si no existe.
    /// Requiere autenticación con rol Admin o Empleado.
    /// </summary>
    [HttpGet("buscar-codigo-barras/{codigo}")]
    [Authorize(Roles = Roles.LecturaInventario)]
    public async Task<IActionResult> BuscarPorCodigoBarras(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return BadRequest(new { message = "El código de barras no puede estar vacío" });

        var producto = await _productoService.ObtenerProductoPorCodigoBarrasAsync(codigo.Trim());
        if (producto == null)
            return NotFound(new { message = "Producto con ese código de barras no encontrado" });

        OcultarDatosInternosSiCorresponde(producto);
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
        catch (StockManager.Domain.Exceptions.ProductoDuplicadoException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
        {
            // Sin este log la causa real quedaba oculta tras el mensaje genérico: ahora aparece
            // completa en el Log stream de Azure.
            _logger.LogError(ex, "Error inesperado al crear el producto '{Nombre}'", request.Nombre);
            return StatusCode(500, new { message = "Error al crear el producto" });
        }
    }

    /// <summary>
    /// Descarga la plantilla vacía para crear productos nuevos (con listas desplegables de
    /// categorías y proveedores, límites de validación e instrucciones).
    /// Requiere autenticación con rol Admin.
    /// </summary>
    [HttpGet("plantilla-excel")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DescargarPlantillaExcel()
    {
        var archivo = await _productoExcelService.GenerarPlantillaAsync();
        return File(archivo, ProductoExcelFormato.TipoContenidoExcel, "plantilla-productos.xlsx");
    }

    /// <summary>
    /// Exporta el inventario actual (productos activos) a Excel.
    /// Requiere autenticación con rol Admin.
    /// </summary>
    [HttpGet("exportar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportarInventario()
    {
        var archivo = await _productoExcelService.ExportarInventarioAsync();
        return File(archivo, ProductoExcelFormato.TipoContenidoExcel, "inventario-productos.xlsx");
    }

    /// <summary>
    /// Importa (crea y edita) productos desde un archivo .xlsx generado por la plantilla o la exportación.
    /// Con <c>soloValidar=true</c> devuelve la vista previa sin guardar nada; con <c>false</c> aplica los
    /// cambios, pero solo si TODAS las filas son válidas (todo o nada).
    /// Requiere autenticación con rol Admin.
    /// </summary>
    [HttpPost("importar")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(MaxBytesArchivoExcel)]
    public async Task<IActionResult> ImportarProductos([FromForm] IFormFile archivo, [FromQuery] bool soloValidar = false)
    {
        if (archivo == null || archivo.Length == 0)
            return BadRequest(new { message = "El archivo no puede estar vacío" });

        if (!archivo.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "El archivo debe ser .xlsx (descarga la plantilla o exporta el inventario)" });

        if (archivo.Length > MaxBytesArchivoExcel)
            return BadRequest(new { message = "El archivo supera el tamaño máximo permitido (5 MB)" });

        try
        {
            using var stream = archivo.OpenReadStream();
            var resultado = await _productoExcelImportador.ProcesarAsync(stream, aplicarCambios: !soloValidar);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
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
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
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

    /// <summary>
    /// Actualiza un producto existente.
    /// No modifica StockActual, solo información general (nombre, categoría, precio, stock mínimo, código de barras).
    /// Requiere autenticación con rol Admin.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActualizarProducto(int id, [FromBody] ActualizarProductoRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var productoResponse = await _productoService.ActualizarProductoAsync(id, request);
            return Ok(productoResponse);
        }
        catch (StockManager.Domain.Exceptions.ProductoNoEncontradoException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (StockManager.Domain.Exceptions.ProductoDuplicadoException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
        {
            // TODO: loguear ex con ILogger cuando se agregue logging
            return StatusCode(500, new { message = "Error al actualizar el producto" });
        }
    }

    /// <summary>
    /// Desactiva un producto (eliminación lógica, no borrado físico).
    /// El producto se marca como inactivo pero permanece en la base de datos.
    /// Requiere autenticación con rol Admin.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DesactivarProducto(int id)
    {
        try
        {
            await _productoService.DesactivarProductoAsync(id);
            return Ok(new { message = "Producto desactivado exitosamente" });
        }
        catch (StockManager.Domain.Exceptions.ProductoNoEncontradoException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
        {
            // TODO: loguear ex con ILogger cuando se agregue logging
            return StatusCode(500, new { message = "Error al desactivar el producto" });
        }
    }

    /// <summary>
    /// Reactiva un producto previamente desactivado.
    /// Requiere autenticación con rol Admin.
    /// </summary>
    [HttpPatch("{id}/reactivar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReactivarProducto(int id)
    {
        try
        {
            await _productoService.ReactivarProductoAsync(id);
            return Ok(new { message = "Producto reactivado exitosamente" });
        }
        catch (StockManager.Domain.Exceptions.ProductoNoEncontradoException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
        {
            // TODO: loguear ex con ILogger cuando se agregue logging
            return StatusCode(500, new { message = "Error al reactivar el producto" });
        }
    }

    /// <summary>
    /// Ajusta manualmente el stock de un producto (ej. llegó mercancía, corrección de un
    /// conteo físico), fuera del flujo de ventas/pedidos. Delta puede ser positivo o negativo;
    /// no puede dejar el stock en negativo.
    /// Requiere autenticación con rol Admin.
    /// </summary>
    [HttpPatch("{id}/ajustar-stock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AjustarStock(int id, [FromBody] AjustarStockRequest request)
    {
        try
        {
            var productoResponse = await _productoService.AjustarStockAsync(id, request.Delta);
            return Ok(productoResponse);
        }
        catch (StockManager.Domain.Exceptions.ProductoNoEncontradoException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (StockManager.Domain.Exceptions.StockInsuficienteException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (StockManager.Domain.Exceptions.ConcurrencyException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is not StockManager.Domain.Exceptions.DomainException and not ArgumentException)
        {
            // TODO: loguear ex con ILogger cuando se agregue logging
            return StatusCode(500, new { message = "Error al ajustar el stock" });
        }
    }
}
