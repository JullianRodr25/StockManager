using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace StockManager.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de gestión de productos.
/// Maneja CRUD, paginación, búsqueda por código de barras, importación desde Excel y gestión de etiquetas.
/// </summary>
public class ProductoService : IProductoService
{
    private readonly AppDbContext _dbContext;
    private readonly IBarcodeService _barcodeService;

    public ProductoService(AppDbContext dbContext, IBarcodeService barcodeService)
    {
        _dbContext = dbContext;
        _barcodeService = barcodeService;
    }

    public async Task<ProductoResponse?> ObtenerProductoPorIdAsync(int id)
    {
        var producto = await _dbContext.Productos.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (producto == null)
            return null;

        return MapearAResponse(producto);
    }

    public async Task<ProductoResponse?> ObtenerProductoPorCodigoBarrasAsync(string codigoBarras)
    {
        var producto = await _dbContext.Productos.AsNoTracking()
            .FirstOrDefaultAsync(p => p.CodigoBarras == codigoBarras);

        if (producto == null)
            return null;

        return MapearAResponse(producto);
    }

    public async Task<(List<ProductoResponse> Items, int Total)> ObtenerProductosPaginadoAsync(
        int pagina,
        int tamanoPagina,
        int? categoriaId = null)
    {
        if (pagina < 1)
            pagina = 1;
        if (tamanoPagina < 1)
            tamanoPagina = 50;

        var query = _dbContext.Productos.AsNoTracking();

        if (categoriaId.HasValue)
            query = query.Where(p => p.CategoriaId == categoriaId.Value);

        var total = await query.CountAsync();
        var skip = (pagina - 1) * tamanoPagina;

        var productos = await query
            .OrderBy(p => p.Id)
            .Skip(skip)
            .Take(tamanoPagina)
            .ToListAsync();

        var items = productos.Select(MapearAResponse).ToList();
        return (items, total);
    }

    public async Task<ProductoResponse> CrearProductoAsync(CrearProductoRequest request)
    {
        // Validar que la categoría existe
        var categoriaExiste = await _dbContext.Categorias
            .AnyAsync(c => c.Id == request.CategoriaId);

        if (!categoriaExiste)
            throw new ArgumentException($"La categoría con ID {request.CategoriaId} no existe.", nameof(request.CategoriaId));

        // Crear el producto usando el factory method
        var producto = Producto.Crear(
            request.Nombre,
            request.CategoriaId,
            request.Precio,
            request.StockInicial,
            request.StockMinimo,
            request.CodigoBarras);

        // Agregar a la base de datos
        _dbContext.Productos.Add(producto);
        await _dbContext.SaveChangesAsync();

        // Si no hay código de barras, generar uno internamente
        if (string.IsNullOrEmpty(producto.CodigoBarras))
        {
            producto.GenerarCodigoBarrasInterno();
            await _dbContext.SaveChangesAsync();
        }

        return MapearAResponse(producto);
    }

    public async Task<ImportarProductosResponse> ImportarProductosDesdeExcelAsync(Stream archivoStream)
    {
        var respuesta = new ImportarProductosResponse
        {
            Errores = new List<ErrorImportacion>()
        };

        try
        {
            using (var workbook = new XLWorkbook(archivoStream))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                    throw new InvalidOperationException("El archivo no contiene hojas de cálculo.");

                var filas = worksheet.RangeUsed().RowsUsed().Skip(1); // Saltar encabezado

                foreach (var fila in filas)
                {
                    try
                    {
                        var numeroFila = fila.RowNumber();
                        respuesta.TotalFilas++;

                        // Leer columnas: Nombre, Categoria, Precio, StockInicial, StockMinimo, CodigoBarras
                        var nombre = fila.Cell(1).GetString()?.Trim();
                        var categoriaNombre = fila.Cell(2).GetString()?.Trim();
                        var precioStr = fila.Cell(3).GetString()?.Trim();
                        var stockInicialStr = fila.Cell(4).GetString()?.Trim();
                        var stockMinimoStr = fila.Cell(5).GetString()?.Trim();
                        var codigoBarras = fila.Cell(6).GetString()?.Trim();

                        // Validar datos obligatorios
                        if (string.IsNullOrEmpty(nombre))
                            throw new InvalidOperationException("El nombre del producto es obligatorio.");
                        if (string.IsNullOrEmpty(categoriaNombre))
                            throw new InvalidOperationException("El nombre de la categoría es obligatorio.");
                        if (!decimal.TryParse(precioStr, out var precio))
                            throw new InvalidOperationException("El precio debe ser un número válido.");
                        if (!int.TryParse(stockInicialStr, out var stockInicial))
                            throw new InvalidOperationException("El stock inicial debe ser un número entero válido.");
                        if (!int.TryParse(stockMinimoStr, out var stockMinimo))
                            throw new InvalidOperationException("El stock mínimo debe ser un número entero válido.");

                        // Buscar o crear categoría
                        var categoria = await _dbContext.Categorias
                            .FirstOrDefaultAsync(c => c.Nombre == categoriaNombre);

                        if (categoria == null)
                        {
                            categoria = Categoria.Crear(categoriaNombre);
                            _dbContext.Categorias.Add(categoria);
                            await _dbContext.SaveChangesAsync();
                        }

                        // Crear producto
                        var producto = Producto.Crear(
                            nombre,
                            categoria.Id,
                            precio,
                            stockInicial,
                            stockMinimo,
                            string.IsNullOrEmpty(codigoBarras) ? null : codigoBarras);

                        _dbContext.Productos.Add(producto);
                        await _dbContext.SaveChangesAsync();

                        // Generar código de barras interno si falta
                        if (string.IsNullOrEmpty(producto.CodigoBarras))
                        {
                            producto.GenerarCodigoBarrasInterno();
                            await _dbContext.SaveChangesAsync();
                        }

                        respuesta.Creados++;
                    }
                    catch (Exception ex)
                    {
                        respuesta.Errores.Add(new ErrorImportacion
                        {
                            Fila = fila.RowNumber(),
                            Mensaje = ex.Message
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            respuesta.Errores.Add(new ErrorImportacion
            {
                Fila = 0,
                Mensaje = $"Error general al procesar el archivo: {ex.Message}"
            });
        }

        return respuesta;
    }

    public async Task<List<EtiquetaPendienteResponse>> ObtenerEtiquetasPendientesAsync()
    {
        var productos = await _dbContext.Productos.AsNoTracking()
            .Where(p => p.EsCodigoGenerado && p.FechaImpresionEtiqueta == null)
            .OrderBy(p => p.Id)
            .ToListAsync();

        return productos.Select(p => new EtiquetaPendienteResponse
        {
            ProductoId = p.Id,
            Nombre = p.Nombre,
            CodigoBarras = p.CodigoBarras ?? "SIN-CODIGO"
        }).ToList();
    }

    public async Task<List<EtiquetaGeneradaResponse>> GenerarEtiquetasAsync(List<int> productosIds)
    {
        var respuesta = new List<EtiquetaGeneradaResponse>();

        var productos = await _dbContext.Productos
            .Where(p => productosIds.Contains(p.Id))
            .ToListAsync();

        foreach (var producto in productos)
        {
            if (string.IsNullOrEmpty(producto.CodigoBarras))
                continue;

            // Generar la imagen del código de barras en base64
            var imagenBase64 = await _barcodeService.GenerarCodigoBarrasBase64Async(producto.CodigoBarras);

            // Marcar que la etiqueta ha sido impresa
            producto.MarcarEtiquetaImpresa();

            respuesta.Add(new EtiquetaGeneradaResponse
            {
                ProductoId = producto.Id,
                CodigoBarras = producto.CodigoBarras,
                ImagenBase64 = imagenBase64
            });
        }

        // Guardar los cambios de FechaImpresionEtiqueta
        if (respuesta.Count > 0)
            await _dbContext.SaveChangesAsync();

        return respuesta;
    }

    private static ProductoResponse MapearAResponse(Producto producto)
    {
        return new ProductoResponse
        {
            Id = producto.Id,
            Nombre = producto.Nombre,
            CategoriaId = producto.CategoriaId,
            Precio = producto.Precio,
            StockActual = producto.StockActual,
            StockMinimo = producto.StockMinimo,
            CodigoBarras = producto.CodigoBarras,
            Activo = producto.Activo
        };
    }
}
