using StockManager.Application.Excel;
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
    private readonly IConfiguracionService _configuracionService;
    private readonly IStockNotificador _stockNotificador;

    public ProductoService(
        AppDbContext dbContext,
        IBarcodeService barcodeService,
        IConfiguracionService configuracionService,
        IStockNotificador stockNotificador)
    {
        _dbContext = dbContext;
        _barcodeService = barcodeService;
        _configuracionService = configuracionService;
        _stockNotificador = stockNotificador;
    }

    public async Task<ProductoResponse?> ObtenerProductoPorIdAsync(int id)
    {
        var producto = await _dbContext.Productos.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (producto == null)
            return null;

        return MapearAResponse(producto, await ObtenerFotosAsync(producto.Id));
    }

    public async Task<ProductoResponse?> ObtenerProductoPorCodigoBarrasAsync(string codigoBarras)
    {
        var producto = await _dbContext.Productos.AsNoTracking()
            .FirstOrDefaultAsync(p => p.CodigoBarras == codigoBarras);

        if (producto == null)
            return null;

        return MapearAResponse(producto, await ObtenerFotosAsync(producto.Id));
    }

    public async Task<List<ProductoResponse>> ObtenerAlertasStockAsync()
    {
        var productos = await _dbContext.Productos
            .AsNoTracking()
            .Where(p => p.Activo && p.StockActual <= p.StockMinimo)
            .OrderBy(p => p.StockActual)
            .ThenBy(p => p.Nombre)
            .ToListAsync();

        var fotosPorProducto = await ObtenerFotosPorProductoIdsAsync(productos.Select(p => p.Id));
        return productos
            .Select(p => MapearAResponse(p, fotosPorProducto.GetValueOrDefault(p.Id)))
            .ToList();
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

        var fotosPorProducto = await ObtenerFotosPorProductoIdsAsync(productos.Select(p => p.Id));
        var items = productos
            .Select(p => MapearAResponse(p, fotosPorProducto.GetValueOrDefault(p.Id)))
            .ToList();
        return (items, total);
    }

    public async Task<(List<ProductoCatalogoResponse> Items, int Total)> ObtenerCatalogoPaginadoAsync(
        int pagina,
        int tamanoPagina,
        int? categoriaId)
    {
        if (pagina < 1)
            pagina = 1;
        if (tamanoPagina < 1)
            tamanoPagina = 50;

        var query = _dbContext.Productos.AsNoTracking()
            .Where(p => p.Activo)
            .Join(
                _dbContext.Categorias.AsNoTracking(),
                producto => producto.CategoriaId,
                categoria => categoria.Id,
                (producto, categoria) => new
                {
                    producto.Id,
                    producto.Nombre,
                    CategoriaNombre = categoria.Nombre,
                    producto.Precio,
                    producto.StockActual,
                    producto.CategoriaId,
                    producto.CalificacionPromedio,
                    producto.TotalResenas,
                    // Unidades vendidas (ventas reales: sin cotizaciones ni canceladas; incluye las
                    // generadas por pedidos de la PWA al entregarse). Se calcula en la misma consulta
                    // para poder ordenar y paginar en SQL, sin traer todo el catálogo a memoria.
                    UnidadesVendidas = _dbContext.DetallesVenta
                        .Where(d => d.ProductoId == producto.Id
                                    && _dbContext.Ventas.Any(v => v.Id == d.VentaId && !v.EsCotizacion && v.Estado != "Cancelada"))
                        .Sum(d => (int?)d.Cantidad) ?? 0
                });

        if (categoriaId.HasValue)
            query = query.Where(p => p.CategoriaId == categoriaId.Value);

        var total = await query.CountAsync();
        var skip = (pagina - 1) * tamanoPagina;

        // Orden "populares primero": lo que hay en stock antes que lo agotado, luego lo más
        // comprado y, a igual cantidad, lo mejor calificado. El Id al final hace el orden estable
        // entre páginas (sin él, productos empatados podrían repetirse o saltarse al paginar).
        var productosPagina = await query
            .OrderByDescending(p => p.StockActual > 0)
            .ThenByDescending(p => p.UnidadesVendidas)
            .ThenByDescending(p => p.CalificacionPromedio ?? 0m)
            .ThenByDescending(p => p.TotalResenas)
            .ThenBy(p => p.Id)
            .Skip(skip)
            .Take(tamanoPagina)
            .ToListAsync();

        // Una sola consulta extra para traer las fotos de todos los productos de esta página
        // (en vez de una consulta por producto, que sería N+1 en un grid de hasta 50 items).
        var fotosPorProducto = await ObtenerFotosPorProductoIdsAsync(productosPagina.Select(p => p.Id));

        var items = productosPagina
            .Select(p => new ProductoCatalogoResponse(
                p.Id,
                p.Nombre,
                p.CategoriaNombre,
                p.Precio,
                p.StockActual > 0,
                fotosPorProducto.GetValueOrDefault(p.Id) ?? new List<ProductoFotoResponse>(),
                p.CalificacionPromedio,
                p.TotalResenas))
            .ToList();

        return (items, total);
    }

    public async Task<List<CategoriaResponse>> ObtenerCategoriasCatalogoAsync()
    {
        // Solo categorías con al menos un producto activo: así aparecen en la PWA a medida que se
        // les cargan productos, y no hay filtros que devuelvan una lista vacía.
        var conteos = await _dbContext.Productos.AsNoTracking()
            .Where(p => p.Activo)
            .GroupBy(p => p.CategoriaId)
            .Select(g => new { CategoriaId = g.Key, Cantidad = g.Count() })
            .ToListAsync();

        var ids = conteos.Select(c => c.CategoriaId).ToList();
        var cantidadPorCategoria = conteos.ToDictionary(c => c.CategoriaId, c => c.Cantidad);

        var categorias = await _dbContext.Categorias.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        return categorias
            .Select(c => new CategoriaResponse
            {
                Id = c.Id,
                Nombre = c.Nombre,
                CantidadProductos = cantidadPorCategoria[c.Id]
            })
            .ToList();
    }

    public async Task<ProductoResponse> CrearProductoAsync(CrearProductoRequest request)
    {
        // Validar que la categoría existe
        var categoriaExiste = await _dbContext.Categorias
            .AnyAsync(c => c.Id == request.CategoriaId);

        if (!categoriaExiste)
            throw new ArgumentException($"La categoría con ID {request.CategoriaId} no existe.", nameof(request.CategoriaId));

        // Validar que el proveedor (si viene) existe
        if (request.ProveedorId.HasValue)
        {
            var proveedorExiste = await _dbContext.Proveedores.AnyAsync(p => p.Id == request.ProveedorId.Value);
            if (!proveedorExiste)
                throw new ArgumentException($"El proveedor con ID {request.ProveedorId.Value} no existe.", nameof(request.ProveedorId));
        }

        // Validar nombre duplicado (case-insensitive)
        var nombreNormalizado = request.Nombre.Trim();
        var existeProducto = await _dbContext.Productos
            .AnyAsync(p => p.Nombre.ToUpper() == nombreNormalizado.ToUpper());
        if (existeProducto)
            throw new Domain.Exceptions.ProductoDuplicadoException(nombreNormalizado);

        // Validar código de barras duplicado ANTES de insertar: el índice único de la BD ya lo
        // impide, pero violarlo lanza un DbUpdateException que el controlador convertía en un
        // 500 sin explicación. Así el usuario recibe un 400 con el motivo exacto (igual que al editar).
        var codigoBarrasNormalizado = string.IsNullOrWhiteSpace(request.CodigoBarras) ? null : request.CodigoBarras.Trim();
        if (codigoBarrasNormalizado != null)
        {
            var existeCodigoBarras = await _dbContext.Productos
                .AnyAsync(p => p.CodigoBarras == codigoBarrasNormalizado);
            if (existeCodigoBarras)
                throw new ArgumentException($"Ya existe un producto con el código de barras '{codigoBarrasNormalizado}'.");
        }

        var tarifaIva = request.TarifaIva
            ?? (await _configuracionService.ObtenerAsync()).TarifaIvaPorDefecto;

        // Crear el producto usando el factory method
        var producto = Producto.Crear(
            request.Nombre,
            request.CategoriaId,
            request.Precio,
            request.StockInicial,
            request.StockMinimo,
            request.AplicaIva,
            tarifaIva,
            request.Costo,
            request.CodigoBarras,
            request.ProveedorId);

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

    public async Task<ProductoResponse> ActualizarProductoAsync(int id, ActualizarProductoRequest request)
    {
        // Buscar el producto
        var producto = await _dbContext.Productos.FindAsync(id);
        if (producto == null)
            throw new Domain.Exceptions.ProductoNoEncontradoException(id);

        // Validar que la categoría existe
        var categoriaExiste = await _dbContext.Categorias
            .AnyAsync(c => c.Id == request.CategoriaId);
        if (!categoriaExiste)
            throw new ArgumentException($"La categoría con ID {request.CategoriaId} no existe.", nameof(request.CategoriaId));

        // Validar que el proveedor (si viene) existe
        if (request.ProveedorId.HasValue)
        {
            var proveedorExiste = await _dbContext.Proveedores.AnyAsync(p => p.Id == request.ProveedorId.Value);
            if (!proveedorExiste)
                throw new ArgumentException($"El proveedor con ID {request.ProveedorId.Value} no existe.", nameof(request.ProveedorId));
        }

        // Validar nombre duplicado EXCLUYENDO el propio producto (case-insensitive)
        var nombreNormalizado = request.Nombre.Trim();
        var existeProducto = await _dbContext.Productos
            .AnyAsync(p => p.Id != id && p.Nombre.ToUpper() == nombreNormalizado.ToUpper());
        if (existeProducto)
            throw new Domain.Exceptions.ProductoDuplicadoException(nombreNormalizado);

        // Si viene código de barras y es distinto al actual, validar que no esté en uso por OTRO producto
        var codigoBarrasNormalizado = string.IsNullOrWhiteSpace(request.CodigoBarras) ? null : request.CodigoBarras.Trim();
        if (!string.IsNullOrWhiteSpace(codigoBarrasNormalizado) && codigoBarrasNormalizado != producto.CodigoBarras)
        {
            var existeCodigoBarras = await _dbContext.Productos
                .AnyAsync(p => p.Id != id && p.CodigoBarras == codigoBarrasNormalizado);
            if (existeCodigoBarras)
                throw new InvalidOperationException($"Ya existe un producto con el código de barras '{codigoBarrasNormalizado}'.");
        }

        // Actualizar información del producto
        producto.ActualizarInformacion(
            request.Nombre,
            request.CategoriaId,
            request.Precio,
            request.Costo,
            request.StockMinimo,
            request.AplicaIva,
            request.TarifaIva ?? producto.TarifaIva,
            codigoBarrasNormalizado,
            request.ProveedorId);

        await _dbContext.SaveChangesAsync();

        return MapearAResponse(producto, await ObtenerFotosAsync(producto.Id));
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

    public async Task DesactivarProductoAsync(int id)
    {
        var producto = await _dbContext.Productos.FindAsync(id);
        if (producto == null)
            throw new Domain.Exceptions.ProductoNoEncontradoException(id);

        producto.Desactivar();
        await _dbContext.SaveChangesAsync();
    }

    public async Task ReactivarProductoAsync(int id)
    {
        var producto = await _dbContext.Productos.FindAsync(id);
        if (producto == null)
            throw new Domain.Exceptions.ProductoNoEncontradoException(id);

        producto.Activar();
        await _dbContext.SaveChangesAsync();
    }

    public async Task<ProductoResponse> AjustarStockAsync(int id, int delta)
    {
        var producto = await _dbContext.Productos.FindAsync(id);
        if (producto == null)
            throw new Domain.Exceptions.ProductoNoEncontradoException(id);

        producto.AjustarStock(delta);

        var movimiento = MovimientoStock.Crear(
            producto.Id,
            delta > 0 ? "Entrada" : "Ajuste",
            Math.Abs(delta),
            "Ajuste");
        _dbContext.MovimientosStock.Add(movimiento);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new Domain.Exceptions.ConcurrencyException(
                "El stock de este producto cambió mientras se procesaba el ajuste. Intenta de nuevo.");
        }

        try
        {
            await _stockNotificador.NotificarCambiosAsync(
                new[] { new CambioStockDto(producto.Id, producto.StockActual) });
        }
        catch
        {
            // Best-effort: un fallo al avisar en tiempo real no debe afectar el ajuste ya guardado.
        }

        return MapearAResponse(producto, await ObtenerFotosAsync(producto.Id));
    }

    /// <summary>
    /// Trae las fotos de un solo producto, ya ordenadas para el carrusel. Para pantallas con
    /// un producto a la vez (detalle, crear, actualizar, ajustar stock); para listas paginadas
    /// usar ObtenerFotosPorProductoIdsAsync y evitar N+1.
    /// </summary>
    private async Task<List<ProductoFotoResponse>> ObtenerFotosAsync(int productoId)
    {
        return await _dbContext.ProductoFotos.AsNoTracking()
            .Where(f => f.ProductoId == productoId)
            .OrderBy(f => f.Orden)
            .Select(f => new ProductoFotoResponse(f.Id, f.Url, f.Orden))
            .ToListAsync();
    }

    /// <summary>
    /// Trae las fotos de varios productos en una sola consulta, agrupadas por ProductoId. Usar
    /// en listas/paginación en vez de llamar ObtenerFotosAsync por cada item.
    /// </summary>
    private async Task<Dictionary<int, List<ProductoFotoResponse>>> ObtenerFotosPorProductoIdsAsync(IEnumerable<int> productoIds)
    {
        var ids = productoIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, List<ProductoFotoResponse>>();

        var fotos = await _dbContext.ProductoFotos.AsNoTracking()
            .Where(f => ids.Contains(f.ProductoId))
            .OrderBy(f => f.Orden)
            .ToListAsync();

        return fotos
            .GroupBy(f => f.ProductoId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => new ProductoFotoResponse(f.Id, f.Url, f.Orden)).ToList());
    }

    private static ProductoResponse MapearAResponse(Producto producto, IReadOnlyList<ProductoFotoResponse>? fotos = null)
    {
        return new ProductoResponse
        {
            Id = producto.Id,
            Nombre = producto.Nombre,
            CategoriaId = producto.CategoriaId,
            Precio = producto.Precio,
            StockActual = producto.StockActual,
            StockMinimo = producto.StockMinimo,
            AplicaIva = producto.AplicaIva,
            TarifaIva = producto.TarifaIva,
            Costo = producto.Costo,
            CodigoBarras = producto.CodigoBarras,
            Activo = producto.Activo,
            ProveedorId = producto.ProveedorId,
            Fotos = fotos?.ToList() ?? new List<ProductoFotoResponse>()
        };
    }
}
