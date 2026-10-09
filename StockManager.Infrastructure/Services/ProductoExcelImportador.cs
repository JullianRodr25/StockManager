using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Excel;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IProductoExcelImportador"/>.
///
/// Decisiones de diseño:
/// - Las reglas del producto (precio, IVA, longitudes, stock no negativo) NO se reescriben aquí:
///   se llama a los mismos métodos del dominio que usa el formulario (Producto.Crear,
///   ActualizarInformacion, AjustarStock). Así el Excel y la pantalla no pueden divergir.
/// - Aquí solo viven las reglas que dependen de "el resto del inventario o del archivo":
///   nombre y código de barras únicos (también entre filas del mismo archivo), que la categoría
///   y el proveedor existan, y qué columnas aplican a productos nuevos o a existentes.
/// - El stock de un producto existente solo cambia con AjusteStock (relativo, +/-): se aplica
///   sobre el stock real del momento, así que no pisa las ventas ocurridas desde que se exportó.
/// - Una fila con Id edita ese producto; sin Id crea uno nuevo. El Id es lo que permite
///   renombrar un producto sin duplicarlo.
/// </summary>
public class ProductoExcelImportador : IProductoExcelImportador
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguracionService _configuracionService;
    private readonly IStockNotificador _stockNotificador;

    public ProductoExcelImportador(
        AppDbContext dbContext,
        IConfiguracionService configuracionService,
        IStockNotificador stockNotificador)
    {
        _dbContext = dbContext;
        _configuracionService = configuracionService;
        _stockNotificador = stockNotificador;
    }

    // Error "esperado" de una fila (dato inválido, regla incumplida): se informa al usuario con su
    // mensaje. Cualquier otra excepción es un fallo real del servidor y sí se deja propagar.
    private sealed class ErrorFilaImportacion : Exception
    {
        public ErrorFilaImportacion(string mensaje) : base(mensaje) { }
    }

    private sealed record ProveedorCatalogo(int Id, bool Activo);

    public async Task<ImportarProductosResponse> ProcesarAsync(Stream archivo, bool aplicarCambios)
    {
        IXLWorksheet hoja;
        XLWorkbook libro;
        try
        {
            libro = new XLWorkbook(archivo);
        }
        catch (Exception)
        {
            throw new InvalidOperationException("No se pudo leer el archivo. Verifica que sea un Excel (.xlsx) válido.");
        }

        using (libro)
        {
            hoja = libro.Worksheets.FirstOrDefault(
                       h => string.Equals(h.Name, ProductoExcelFormato.HojaProductos, StringComparison.OrdinalIgnoreCase))
                   ?? libro.Worksheets.FirstOrDefault()
                   ?? throw new InvalidOperationException("El archivo no contiene hojas de cálculo.");

            ValidarEncabezados(hoja);

            // Solo cuentan filas con contenido real: las reglas de validación de la plantilla
            // marcan miles de filas vacías como "con formato" y no deben procesarse.
            var ultimaFila = hoja.LastRowUsed(XLCellsUsedOptions.Contents)?.RowNumber() ?? ProductoExcelFormato.FilaEncabezado;
            if (ultimaFila < ProductoExcelFormato.PrimeraFilaDatos)
                throw new InvalidOperationException("El archivo no contiene filas de productos.");

            var respuesta = await ProcesarFilasAsync(hoja, ultimaFila, aplicarCambios);
            return respuesta;
        }
    }

    private static void ValidarEncabezados(IXLWorksheet hoja)
    {
        var nombre = hoja.Cell(ProductoExcelFormato.FilaEncabezado, ProductoExcelFormato.ColNombre).GetString().Trim();
        var categoria = hoja.Cell(ProductoExcelFormato.FilaEncabezado, ProductoExcelFormato.ColCategoria).GetString().Trim();

        var formatoValido =
            string.Equals(nombre, "Nombre", StringComparison.OrdinalIgnoreCase)
            && categoria.StartsWith("Categor", StringComparison.OrdinalIgnoreCase);

        if (!formatoValido)
        {
            throw new InvalidOperationException(
                "El archivo no tiene el formato esperado. Descarga la plantilla desde Inventario → Exportar y no cambies los encabezados de la fila 1.");
        }
    }

    private async Task<ImportarProductosResponse> ProcesarFilasAsync(IXLWorksheet hoja, int ultimaFila, bool aplicarCambios)
    {
        var respuesta = new ImportarProductosResponse();

        // --- Datos de referencia, cargados una sola vez ---------------------------------------
        var categorias = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in await _dbContext.Categorias.AsNoTracking().Select(c => new { c.Id, c.Nombre }).ToListAsync())
            categorias.TryAdd(c.Nombre.Trim(), c.Id);

        var proveedores = new Dictionary<string, ProveedorCatalogo>(StringComparer.OrdinalIgnoreCase);
        foreach (var pr in await _dbContext.Proveedores.AsNoTracking().Select(pr => new { pr.Id, pr.Nombre, pr.Activo }).ToListAsync())
            proveedores.TryAdd(pr.Nombre.Trim(), new ProveedorCatalogo(pr.Id, pr.Activo));

        // Nombres y códigos ya usados por cualquier producto (activo o no) -> Id del dueño.
        // Se van actualizando a medida que se aceptan filas, para detectar también los choques
        // entre filas del mismo archivo.
        var nombresEnUso = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var codigosEnUso = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in await _dbContext.Productos.AsNoTracking().Select(p => new { p.Id, p.Nombre, p.CodigoBarras }).ToListAsync())
        {
            nombresEnUso.TryAdd(p.Nombre.Trim(), p.Id);
            if (!string.IsNullOrWhiteSpace(p.CodigoBarras))
                codigosEnUso.TryAdd(p.CodigoBarras.Trim(), p.Id);
        }

        var tarifaIvaGeneral = (await _configuracionService.ObtenerAsync()).TarifaIvaPorDefecto;

        // Productos que el archivo quiere editar (con Id), ya rastreados por EF para poder modificarlos.
        var idsAEditar = new HashSet<int>();
        for (var numeroFila = ProductoExcelFormato.PrimeraFilaDatos; numeroFila <= ultimaFila; numeroFila++)
        {
            var celdaId = hoja.Cell(numeroFila, ProductoExcelFormato.ColId);
            if (!EsVacia(celdaId) && celdaId.DataType == XLDataType.Number)
            {
                var id = celdaId.GetDouble();
                if (id > 0 && id <= int.MaxValue && id == Math.Truncate(id))
                    idsAEditar.Add((int)id);
            }
        }

        var productosPorId = idsAEditar.Count == 0
            ? new Dictionary<int, Producto>()
            : await _dbContext.Productos.Where(p => idsAEditar.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        // --- Pasada por las filas -------------------------------------------------------------
        var nuevos = new List<Producto>();
        var ajustados = new List<Producto>();
        var idTemporalNuevos = 0; // negativo y único por fila nueva: "dueño" provisional de su nombre/código

        for (var numeroFila = ProductoExcelFormato.PrimeraFilaDatos; numeroFila <= ultimaFila; numeroFila++)
        {
            if (FilaVacia(hoja, numeroFila))
                continue;

            respuesta.TotalFilas++;

            try
            {
                var fila = LeerFila(hoja, numeroFila);

                var categoriaId = ResolverCategoria(fila.Categoria, categorias);
                if (fila.Precio <= 0)
                    throw new ErrorFilaImportacion("El precio debe ser mayor a 0.");

                var aplicaIva = fila.TarifaIva != 0m;
                var tarifaIva = aplicaIva ? (fila.TarifaIva ?? tarifaIvaGeneral) : 0m;

                if (fila.Id is null)
                {
                    idTemporalNuevos--;
                    var nuevo = CrearProductoNuevo(fila, categoriaId, aplicaIva, tarifaIva, proveedores, nombresEnUso, codigosEnUso, idTemporalNuevos);
                    _dbContext.Productos.Add(nuevo);
                    nuevos.Add(nuevo);
                    respuesta.Creados++;
                }
                else
                {
                    if (!productosPorId.TryGetValue(fila.Id.Value, out var existente))
                        throw new ErrorFilaImportacion($"El Id {fila.Id.Value} no existe. No cambies la columna Id.");

                    var huboCambios = EditarProductoExistente(
                        existente, fila, categoriaId, aplicaIva, tarifaIva, proveedores, nombresEnUso, codigosEnUso, out var seAjustoStock);

                    if (seAjustoStock)
                        ajustados.Add(existente);

                    if (huboCambios) respuesta.Modificados++;
                    else respuesta.SinCambios++;
                }
            }
            catch (Exception ex) when (ex is ErrorFilaImportacion or ArgumentException or DomainException)
            {
                respuesta.Errores.Add(new ErrorImportacion { Fila = numeroFila, Mensaje = LimpiarMensaje(ex) });
            }
        }

        // --- Todo o nada ----------------------------------------------------------------------
        if (respuesta.Errores.Count > 0 || !aplicarCambios)
        {
            // Se descarta todo lo que se haya modificado en memoria: no se guarda nada.
            _dbContext.ChangeTracker.Clear();
            respuesta.Aplicado = false;
            return respuesta;
        }

        await GuardarAsync(nuevos, ajustados);
        respuesta.Aplicado = true;
        return respuesta;
    }

    // ---------------------------------------------------------------------------------------
    // Creación y edición de una fila
    // ---------------------------------------------------------------------------------------

    private static Producto CrearProductoNuevo(
        FilaProducto fila,
        int categoriaId,
        bool aplicaIva,
        decimal tarifaIva,
        Dictionary<string, ProveedorCatalogo> proveedores,
        Dictionary<string, int> nombresEnUso,
        Dictionary<string, int> codigosEnUso,
        int idTemporal)
    {
        if (fila.AjusteStock is not null and not 0)
            throw new ErrorFilaImportacion("AjusteStock solo aplica a productos que ya existen. Para uno nuevo usa StockInicial.");

        if (fila.StockInicial is null)
            throw new ErrorFilaImportacion("El stock inicial es obligatorio en los productos nuevos.");
        if (fila.StockInicial <= 0)
            throw new ErrorFilaImportacion("El stock inicial debe ser mayor a 0: no se crean productos con stock cero.");

        ReservarNombre(fila.Nombre, idTemporal, nombresEnUso);
        ReservarCodigo(fila.CodigoBarras, idTemporal, codigosEnUso);
        var proveedorId = ResolverProveedor(fila.Proveedor, proveedores, proveedorActualId: null);

        // Producto.Crear aplica las reglas del dominio (longitudes, costo, IVA, stock no negativo...).
        return Producto.Crear(
            fila.Nombre,
            categoriaId,
            fila.Precio,
            fila.StockInicial.Value,
            fila.StockMinimo,
            aplicaIva,
            tarifaIva,
            fila.Costo,
            fila.CodigoBarras,
            proveedorId);
    }

    private bool EditarProductoExistente(
        Producto producto,
        FilaProducto fila,
        int categoriaId,
        bool aplicaIva,
        decimal tarifaIva,
        Dictionary<string, ProveedorCatalogo> proveedores,
        Dictionary<string, int> nombresEnUso,
        Dictionary<string, int> codigosEnUso,
        out bool seAjustoStock)
    {
        seAjustoStock = false;

        if (!producto.Activo)
            throw new ErrorFilaImportacion($"El producto '{producto.Nombre}' está inactivo. Reactívalo desde Inventario antes de editarlo.");

        if (fila.StockInicial is not null)
            throw new ErrorFilaImportacion("StockInicial solo aplica a productos nuevos. Para cambiar el stock de uno existente usa AjusteStock.");

        var proveedorId = ResolverProveedor(fila.Proveedor, proveedores, producto.ProveedorId);

        var nombreCambio = !string.Equals(fila.Nombre, producto.Nombre, StringComparison.Ordinal);
        if (nombreCambio)
        {
            ReservarNombre(fila.Nombre, producto.Id, nombresEnUso);
            LiberarSiEsDe(producto.Nombre, producto.Id, nombresEnUso, fila.Nombre);
        }

        // Una celda de código vacía conserva el código actual (ActualizarInformacion lo respeta).
        var codigoCambio = fila.CodigoBarras is not null
            && !string.Equals(fila.CodigoBarras, producto.CodigoBarras, StringComparison.Ordinal);
        if (codigoCambio)
        {
            ReservarCodigo(fila.CodigoBarras, producto.Id, codigosEnUso);
            if (producto.CodigoBarras is not null)
                LiberarSiEsDe(producto.CodigoBarras, producto.Id, codigosEnUso, fila.CodigoBarras!);
        }

        var infoCambio =
            nombreCambio
            || codigoCambio
            || categoriaId != producto.CategoriaId
            || fila.Precio != producto.Precio
            || fila.Costo != producto.Costo
            || fila.StockMinimo != producto.StockMinimo
            || aplicaIva != producto.AplicaIva
            || tarifaIva != producto.TarifaIva
            || proveedorId != producto.ProveedorId;

        if (infoCambio)
        {
            producto.ActualizarInformacion(
                fila.Nombre,
                categoriaId,
                fila.Precio,
                fila.Costo,
                fila.StockMinimo,
                aplicaIva,
                tarifaIva,
                fila.CodigoBarras,
                proveedorId);
        }

        var ajuste = fila.AjusteStock ?? 0;
        if (ajuste != 0)
        {
            // Lanza StockInsuficienteException si dejaría el stock en negativo.
            producto.AjustarStock(ajuste);

            _dbContext.MovimientosStock.Add(MovimientoStock.Crear(
                producto.Id,
                ajuste > 0 ? "Entrada" : "Ajuste",
                Math.Abs(ajuste),
                "Ajuste"));
            seAjustoStock = true;
        }

        return infoCambio || seAjustoStock;
    }

    // ---------------------------------------------------------------------------------------
    // Guardado (todo en una transacción)
    // ---------------------------------------------------------------------------------------

    private async Task GuardarAsync(List<Producto> nuevos, List<Producto> ajustados)
    {
        await using var transaccion = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            await _dbContext.SaveChangesAsync();

            // El código de barras interno se deriva del Id, que solo existe después de insertar.
            var sinCodigo = nuevos.Where(p => string.IsNullOrEmpty(p.CodigoBarras)).ToList();
            foreach (var producto in sinCodigo)
                producto.GenerarCodigoBarrasInterno();
            if (sinCodigo.Count > 0)
                await _dbContext.SaveChangesAsync();

            await transaccion.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException(
                "Algunos productos cambiaron mientras se procesaba el archivo (por ejemplo por una venta). No se guardó nada: vuelve a subirlo.");
        }

        if (ajustados.Count == 0)
            return;

        try
        {
            await _stockNotificador.NotificarCambiosAsync(
                ajustados.Select(p => new CambioStockDto(p.Id, p.StockActual)).ToList());
        }
        catch
        {
            // Best-effort: un fallo al avisar en tiempo real no debe afectar lo ya guardado.
        }
    }

    // ---------------------------------------------------------------------------------------
    // Unicidad (nombre y código de barras)
    // ---------------------------------------------------------------------------------------

    private static void ReservarNombre(string nombre, int duenoId, Dictionary<string, int> enUso)
    {
        if (enUso.TryGetValue(nombre, out var otroId) && otroId != duenoId)
            throw new ProductoDuplicadoException(nombre);
        enUso[nombre] = duenoId;
    }

    private static void ReservarCodigo(string? codigo, int duenoId, Dictionary<string, int> enUso)
    {
        if (codigo is null)
            return;
        if (enUso.TryGetValue(codigo, out var otroId) && otroId != duenoId)
            throw new ErrorFilaImportacion($"Ya existe un producto con el código de barras '{codigo}'.");
        enUso[codigo] = duenoId;
    }

    // Al renombrar un producto, su nombre anterior queda libre para otras filas del archivo.
    private static void LiberarSiEsDe(string anterior, int duenoId, Dictionary<string, int> enUso, string nuevo)
    {
        if (string.Equals(anterior, nuevo, StringComparison.OrdinalIgnoreCase))
            return;
        if (enUso.TryGetValue(anterior, out var id) && id == duenoId)
            enUso.Remove(anterior);
    }

    // ---------------------------------------------------------------------------------------
    // Resolución de categoría y proveedor
    // ---------------------------------------------------------------------------------------

    private static int ResolverCategoria(string nombre, Dictionary<string, int> categorias)
    {
        if (!categorias.TryGetValue(nombre, out var id))
            throw new ErrorFilaImportacion(
                $"La categoría '{nombre}' no existe en el maestro de categorías. Créala en Configuración → Categorías.");
        return id;
    }

    // Vacío = sin proveedor. Un proveedor inactivo solo se acepta si el producto ya lo tenía
    // (así un producto no queda "sin editar" porque su proveedor se dio de baja después).
    private static int? ResolverProveedor(string? nombre, Dictionary<string, ProveedorCatalogo> proveedores, int? proveedorActualId)
    {
        if (nombre is null)
            return null;

        if (!proveedores.TryGetValue(nombre, out var proveedor))
            throw new ErrorFilaImportacion($"El proveedor '{nombre}' no existe.");

        if (!proveedor.Activo && proveedor.Id != proveedorActualId)
            throw new ErrorFilaImportacion($"El proveedor '{nombre}' está inactivo.");

        return proveedor.Id;
    }

    // ---------------------------------------------------------------------------------------
    // Lectura de celdas
    // ---------------------------------------------------------------------------------------

    private sealed record FilaProducto(
        int? Id,
        string Nombre,
        string Categoria,
        decimal Precio,
        int? StockInicial,
        int StockMinimo,
        string? CodigoBarras,
        decimal? TarifaIva,
        decimal Costo,
        string? Proveedor,
        int? AjusteStock);

    private static FilaProducto LeerFila(IXLWorksheet hoja, int numeroFila)
    {
        IXLCell Celda(int columna) => hoja.Cell(numeroFila, columna);

        var nombre = LeerTexto(Celda(ProductoExcelFormato.ColNombre))
            ?? throw new ErrorFilaImportacion("El nombre del producto es obligatorio.");
        var categoria = LeerTexto(Celda(ProductoExcelFormato.ColCategoria))
            ?? throw new ErrorFilaImportacion("La categoría es obligatoria.");
        var precio = LeerDecimal(Celda(ProductoExcelFormato.ColPrecio), "El precio")
            ?? throw new ErrorFilaImportacion("El precio es obligatorio.");
        var stockMinimo = LeerEntero(Celda(ProductoExcelFormato.ColStockMinimo), "El stock mínimo")
            ?? throw new ErrorFilaImportacion("El stock mínimo es obligatorio.");

        return new FilaProducto(
            Id: LeerEntero(Celda(ProductoExcelFormato.ColId), "El Id"),
            Nombre: nombre,
            Categoria: categoria,
            Precio: precio,
            StockInicial: LeerEntero(Celda(ProductoExcelFormato.ColStockInicial), "El stock inicial"),
            StockMinimo: stockMinimo,
            CodigoBarras: LeerTexto(Celda(ProductoExcelFormato.ColCodigoBarras)),
            TarifaIva: LeerDecimal(Celda(ProductoExcelFormato.ColTarifaIva), "La tarifa de IVA"),
            Costo: LeerDecimal(Celda(ProductoExcelFormato.ColCosto), "El costo") ?? 0m,
            Proveedor: LeerTexto(Celda(ProductoExcelFormato.ColProveedor)),
            AjusteStock: LeerEntero(Celda(ProductoExcelFormato.ColAjusteStock), "El ajuste de stock"));
    }

    private static bool EsVacia(IXLCell celda) =>
        celda.IsEmpty() || string.IsNullOrWhiteSpace(celda.GetString());

    private static bool FilaVacia(IXLWorksheet hoja, int numeroFila)
    {
        // StockActual (solo lectura) no cuenta: una fila con solo ese dato no es una fila de producto.
        for (var columna = 1; columna <= ProductoExcelFormato.TotalColumnasExportacion; columna++)
        {
            if (columna == ProductoExcelFormato.ColStockActual)
                continue;
            if (!EsVacia(hoja.Cell(numeroFila, columna)))
                return false;
        }
        return true;
    }

    private static string? LeerTexto(IXLCell celda)
    {
        var texto = celda.GetString()?.Trim();
        return string.IsNullOrEmpty(texto) ? null : texto;
    }

    // Los números se aceptan solo si la celda ES un número. Un texto como "68.000" es ambiguo
    // (¿68 o 68 mil?) y adivinarlo cambiaría precios en silencio: es más seguro rechazarlo.
    private static decimal? LeerDecimal(IXLCell celda, string campo)
    {
        if (EsVacia(celda))
            return null;

        if (celda.DataType != XLDataType.Number)
            throw new ErrorFilaImportacion($"{campo} debe ser un número (escríbelo sin texto ni puntos de miles).");

        return (decimal)celda.GetDouble();
    }

    private static int? LeerEntero(IXLCell celda, string campo)
    {
        var valor = LeerDecimal(celda, campo);
        if (valor is null)
            return null;

        if (valor != Math.Truncate(valor.Value))
            throw new ErrorFilaImportacion($"{campo} debe ser un número entero.");
        if (valor < int.MinValue || valor > int.MaxValue)
            throw new ErrorFilaImportacion($"{campo} está fuera del rango permitido.");

        return (int)valor.Value;
    }

    // ArgumentException.Message termina en " (Parameter 'x')", que no le sirve al usuario.
    private static string LimpiarMensaje(Exception ex)
    {
        var mensaje = ex.Message;
        var indice = mensaje.IndexOf(" (Parameter '", StringComparison.Ordinal);
        return indice > 0 ? mensaje[..indice] : mensaje;
    }
}
