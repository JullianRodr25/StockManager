using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using StockManager.Application.Excel;
using StockManager.Application.Services;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IProductoExcelService"/> con ClosedXML.
///
/// Las listas desplegables y los límites numéricos se incluyen en el propio Excel para que quien
/// lo llena reciba el error al escribir, en la celda, y no recién al subir el archivo. Son una
/// ayuda: la validación que manda sigue siendo la del servidor al importar.
/// </summary>
public class ProductoExcelService : IProductoExcelService
{
    private const string ColorEncabezadoObligatorio = "#F5B800";
    private const string ColorEncabezadoOpcional = "#FCE8A6";
    private const string ColorEncabezadoSoloLectura = "#D9D9D9";
    private const string ColorFilaSoloLectura = "#F2F2F2";
    private const string ColorTextoEncabezado = "#1B2A41";

    private static readonly double[] AnchosColumna =
    {
        42, // Nombre
        22, // Categoria
        14, // Precio
        14, // StockInicial
        14, // StockMinimo
        20, // CodigoBarras
        12, // TarifaIva
        14, // Costo
        24, // Proveedor
        14, // StockActual
        14, // AjusteStock
        10, // Id
    };

    private readonly AppDbContext _dbContext;

    public ProductoExcelService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<byte[]> GenerarPlantillaAsync()
    {
        var catalogos = await CargarCatalogosAsync();

        using var libro = new XLWorkbook();
        var hojas = CrearHojas(libro);

        EscribirEncabezados(hojas.Productos, ProductoExcelFormato.TotalColumnasPlantilla);
        EscribirCatalogos(hojas, catalogos);
        AplicarValidaciones(
            hojas,
            catalogos,
            ultimaFila: ProductoExcelFormato.FilasMinimasConValidacion,
            incluirColumnasDeEdicion: false);
        EscribirInstrucciones(hojas.Instrucciones, esExportacion: false);

        return Guardar(libro);
    }

    public async Task<byte[]> ExportarInventarioAsync()
    {
        var catalogos = await CargarCatalogosAsync();

        // Para mostrar el proveedor de cada producto se necesita el nombre incluso si el proveedor
        // ya está inactivo (la lista desplegable solo ofrece los vigentes).
        var proveedoresPorId = await _dbContext.Proveedores.AsNoTracking()
            .ToDictionaryAsync(p => p.Id, p => p.Nombre);
        var categoriasPorId = catalogos.Categorias.ToDictionary(c => c.Id, c => c.Nombre);

        var productos = await _dbContext.Productos.AsNoTracking()
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .Select(p => new
            {
                p.Id,
                p.Nombre,
                p.CategoriaId,
                p.Precio,
                p.StockActual,
                p.StockMinimo,
                p.CodigoBarras,
                p.AplicaIva,
                p.TarifaIva,
                p.Costo,
                p.ProveedorId,
            })
            .ToListAsync();

        using var libro = new XLWorkbook();
        var hojas = CrearHojas(libro);

        EscribirEncabezados(hojas.Productos, ProductoExcelFormato.TotalColumnasExportacion);
        EscribirCatalogos(hojas, catalogos);

        var fila = ProductoExcelFormato.PrimeraFilaDatos;
        foreach (var p in productos)
        {
            hojas.Productos.Cell(fila, ProductoExcelFormato.ColNombre).Value = p.Nombre;
            hojas.Productos.Cell(fila, ProductoExcelFormato.ColCategoria).Value =
                categoriasPorId.GetValueOrDefault(p.CategoriaId) ?? string.Empty;
            hojas.Productos.Cell(fila, ProductoExcelFormato.ColPrecio).Value = p.Precio;
            hojas.Productos.Cell(fila, ProductoExcelFormato.ColStockMinimo).Value = p.StockMinimo;
            hojas.Productos.Cell(fila, ProductoExcelFormato.ColCodigoBarras).Value = p.CodigoBarras;

            // Se exporta siempre explícita (0 = exento) para que al volver a subir el archivo no se
            // interprete una celda vacía como "usar el IVA general".
            hojas.Productos.Cell(fila, ProductoExcelFormato.ColTarifaIva).Value = p.AplicaIva ? p.TarifaIva : 0m;
            hojas.Productos.Cell(fila, ProductoExcelFormato.ColCosto).Value = p.Costo;
            hojas.Productos.Cell(fila, ProductoExcelFormato.ColProveedor).Value =
                p.ProveedorId.HasValue ? proveedoresPorId.GetValueOrDefault(p.ProveedorId.Value) : null;
            hojas.Productos.Cell(fila, ProductoExcelFormato.ColStockActual).Value = p.StockActual;
            hojas.Productos.Cell(fila, ProductoExcelFormato.ColId).Value = p.Id;
            fila++;
        }

        var ultimaFilaConDatos = fila - 1;
        if (productos.Count > 0)
        {
            hojas.Productos
                .Range(
                    ProductoExcelFormato.PrimeraFilaDatos,
                    ProductoExcelFormato.ColStockActual,
                    ultimaFilaConDatos,
                    ProductoExcelFormato.ColStockActual)
                .Style.Fill.BackgroundColor = XLColor.FromHtml(ColorFilaSoloLectura);
            hojas.Productos
                .Range(
                    ProductoExcelFormato.PrimeraFilaDatos,
                    ProductoExcelFormato.ColId,
                    ultimaFilaConDatos,
                    ProductoExcelFormato.ColId)
                .Style.Fill.BackgroundColor = XLColor.FromHtml(ColorFilaSoloLectura);

            hojas.Productos
                .Range(
                    ProductoExcelFormato.FilaEncabezado,
                    1,
                    ultimaFilaConDatos,
                    ProductoExcelFormato.TotalColumnasExportacion)
                .SetAutoFilter();
        }

        AplicarValidaciones(
            hojas,
            catalogos,
            ultimaFila: Math.Max(ProductoExcelFormato.FilasMinimasConValidacion, ultimaFilaConDatos + 500),
            incluirColumnasDeEdicion: true);
        EscribirInstrucciones(hojas.Instrucciones, esExportacion: true);

        return Guardar(libro);
    }

    // ---------------------------------------------------------------------------------------
    // Datos de referencia (categorías y proveedores vigentes)
    // ---------------------------------------------------------------------------------------

    private sealed record ItemCatalogo(int Id, string Nombre);

    private sealed record Catalogos(List<ItemCatalogo> Categorias, List<ItemCatalogo> ProveedoresActivos);

    private sealed record Hojas(
        IXLWorksheet Productos,
        IXLWorksheet Instrucciones,
        IXLWorksheet Categorias,
        IXLWorksheet Proveedores);

    private async Task<Catalogos> CargarCatalogosAsync()
    {
        var categorias = await _dbContext.Categorias.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new ItemCatalogo(c.Id, c.Nombre))
            .ToListAsync();

        var proveedores = await _dbContext.Proveedores.AsNoTracking()
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .Select(p => new ItemCatalogo(p.Id, p.Nombre))
            .ToListAsync();

        return new Catalogos(categorias, proveedores);
    }

    // ---------------------------------------------------------------------------------------
    // Construcción del libro
    // ---------------------------------------------------------------------------------------

    // Las cuatro hojas se crean de entrada y en este orden: las reglas de validación de la hoja
    // Productos apuntan a rangos de las hojas de categorías y proveedores, que por tanto tienen que existir.
    private static Hojas CrearHojas(XLWorkbook libro)
    {
        var productos = libro.Worksheets.Add(ProductoExcelFormato.HojaProductos);
        var instrucciones = libro.Worksheets.Add(ProductoExcelFormato.HojaInstrucciones);
        var categorias = libro.Worksheets.Add(ProductoExcelFormato.HojaCategorias);
        var proveedores = libro.Worksheets.Add(ProductoExcelFormato.HojaProveedores);
        return new Hojas(productos, instrucciones, categorias, proveedores);
    }

    private static void EscribirEncabezados(IXLWorksheet hoja, int totalColumnas)
    {
        for (var columna = 1; columna <= totalColumnas; columna++)
        {
            var celda = hoja.Cell(ProductoExcelFormato.FilaEncabezado, columna);
            celda.Value = ProductoExcelFormato.Encabezados[columna - 1];
            celda.Style.Font.Bold = true;
            celda.Style.Font.FontColor = XLColor.FromHtml(ColorTextoEncabezado);
            celda.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var color = columna switch
            {
                ProductoExcelFormato.ColStockActual or ProductoExcelFormato.ColId => ColorEncabezadoSoloLectura,
                _ when Array.IndexOf(ProductoExcelFormato.ColumnasObligatorias, columna) >= 0 => ColorEncabezadoObligatorio,
                _ => ColorEncabezadoOpcional,
            };
            celda.Style.Fill.BackgroundColor = XLColor.FromHtml(color);

            hoja.Column(columna).Width = AnchosColumna[columna - 1];
        }

        // El código de barras se guarda como texto: si Excel lo tratara como número, un EAN de 13
        // dígitos se mostraría en notación científica y los ceros a la izquierda se perderían.
        hoja.Column(ProductoExcelFormato.ColCodigoBarras).Style.NumberFormat.Format = "@";

        hoja.SheetView.FreezeRows(ProductoExcelFormato.FilaEncabezado);
    }

    private static void EscribirCatalogos(Hojas hojas, Catalogos catalogos)
    {
        EscribirListaDeReferencia(hojas.Categorias, "Categoria", catalogos.Categorias);
        EscribirListaDeReferencia(hojas.Proveedores, "Proveedor", catalogos.ProveedoresActivos);
    }

    private static void EscribirListaDeReferencia(IXLWorksheet hoja, string encabezado, List<ItemCatalogo> items)
    {
        var celdaEncabezado = hoja.Cell(1, 1);
        celdaEncabezado.Value = encabezado;
        celdaEncabezado.Style.Font.Bold = true;
        celdaEncabezado.Style.Fill.BackgroundColor = XLColor.FromHtml(ColorEncabezadoSoloLectura);
        hoja.Column(1).Width = 40;

        for (var i = 0; i < items.Count; i++)
            hoja.Cell(i + 2, 1).Value = items[i].Nombre;
    }

    private static void AplicarValidaciones(Hojas hojas, Catalogos catalogos, int ultimaFila, bool incluirColumnasDeEdicion)
    {
        var hoja = hojas.Productos;
        var primera = ProductoExcelFormato.PrimeraFilaDatos;

        // Nombre: obligatorio y de hasta 200 caracteres (mismo límite que Producto.Crear).
        var nombre = NuevaRegla(hoja, ProductoExcelFormato.ColNombre, primera, ultimaFila,
            "Nombre no válido", "El nombre es obligatorio y puede tener hasta 200 caracteres.");
        nombre.AllowedValues = XLAllowedValues.TextLength;
        nombre.Operator = XLOperator.Between;
        nombre.MinValue = "1";
        nombre.MaxValue = "200";

        // Categoría: solo las del maestro. Con "Stop" Excel impide escribir una que no exista.
        if (catalogos.Categorias.Count > 0)
        {
            var categoria = NuevaRegla(hoja, ProductoExcelFormato.ColCategoria, primera, ultimaFila,
                "Categoría no válida", "Elige una categoría de la lista. Si falta, créala en Configuración → Categorías.");
            categoria.List(hojas.Categorias.Range(2, 1, catalogos.Categorias.Count + 1, 1), true);
        }

        var precio = NuevaRegla(hoja, ProductoExcelFormato.ColPrecio, primera, ultimaFila,
            "Precio no válido", "El precio debe ser un número mayor a 0.");
        precio.AllowedValues = XLAllowedValues.Decimal;
        precio.Operator = XLOperator.GreaterThan;
        precio.Value = "0";

        var stockInicial = NuevaRegla(hoja, ProductoExcelFormato.ColStockInicial, primera, ultimaFila,
            "Stock inicial no válido", "Para productos nuevos, el stock inicial debe ser un entero mayor a 0.");
        stockInicial.AllowedValues = XLAllowedValues.WholeNumber;
        stockInicial.Operator = XLOperator.GreaterThan;
        stockInicial.Value = "0";

        var stockMinimo = NuevaRegla(hoja, ProductoExcelFormato.ColStockMinimo, primera, ultimaFila,
            "Stock mínimo no válido", "El stock mínimo debe ser un entero de 0 en adelante.");
        stockMinimo.AllowedValues = XLAllowedValues.WholeNumber;
        stockMinimo.Operator = XLOperator.EqualOrGreaterThan;
        stockMinimo.Value = "0";

        var codigoBarras = NuevaRegla(hoja, ProductoExcelFormato.ColCodigoBarras, primera, ultimaFila,
            "Código no válido", "El código de barras puede tener hasta 50 caracteres.");
        codigoBarras.AllowedValues = XLAllowedValues.TextLength;
        codigoBarras.Operator = XLOperator.Between;
        codigoBarras.MinValue = "1";
        codigoBarras.MaxValue = "50";

        var tarifaIva = NuevaRegla(hoja, ProductoExcelFormato.ColTarifaIva, primera, ultimaFila,
            "Tarifa de IVA no válida", "La tarifa debe estar entre 0 y 100. Vacía = IVA general; 0 = exento.");
        tarifaIva.AllowedValues = XLAllowedValues.Decimal;
        tarifaIva.Operator = XLOperator.Between;
        tarifaIva.MinValue = "0";
        tarifaIva.MaxValue = "100";

        var costo = NuevaRegla(hoja, ProductoExcelFormato.ColCosto, primera, ultimaFila,
            "Costo no válido", "El costo debe ser un número de 0 en adelante.");
        costo.AllowedValues = XLAllowedValues.Decimal;
        costo.Operator = XLOperator.EqualOrGreaterThan;
        costo.Value = "0";

        if (catalogos.ProveedoresActivos.Count > 0)
        {
            var proveedor = NuevaRegla(hoja, ProductoExcelFormato.ColProveedor, primera, ultimaFila,
                "Proveedor no válido", "Elige un proveedor de la lista o deja la celda vacía.");
            proveedor.List(hojas.Proveedores.Range(2, 1, catalogos.ProveedoresActivos.Count + 1, 1), true);
        }

        if (incluirColumnasDeEdicion)
        {
            // Ajuste relativo (+5 / -2): se aplica sobre el stock real del momento, así no pisa las
            // ventas ocurridas entre que se exportó el archivo y se volvió a subir.
            var ajuste = NuevaRegla(hoja, ProductoExcelFormato.ColAjusteStock, primera, ultimaFila,
                "Ajuste no válido", "Escribe un entero distinto de 0 (por ejemplo 5 o -2) o deja la celda vacía.");
            ajuste.AllowedValues = XLAllowedValues.WholeNumber;
            ajuste.Operator = XLOperator.NotEqualTo;
            ajuste.Value = "0";
        }
    }

    private static IXLDataValidation NuevaRegla(
        IXLWorksheet hoja, int columna, int primeraFila, int ultimaFila, string titulo, string mensaje)
    {
        var regla = hoja.Range(primeraFila, columna, ultimaFila, columna).CreateDataValidation();
        regla.IgnoreBlanks = true;
        regla.ShowErrorMessage = true;
        regla.ErrorStyle = XLErrorStyle.Stop;
        regla.ErrorTitle = titulo;
        regla.ErrorMessage = mensaje;
        return regla;
    }

    private static void EscribirInstrucciones(IXLWorksheet hoja, bool esExportacion)
    {
        var lineas = esExportacion ? InstruccionesExportacion() : InstruccionesPlantilla();

        for (var i = 0; i < lineas.Count; i++)
        {
            var celda = hoja.Cell(i + 1, 1);
            celda.Value = lineas[i];
            celda.Style.Alignment.WrapText = true;
            celda.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

            // Los títulos de sección se escriben en MAYÚSCULAS en la lista.
            if (lineas[i].Length > 0 && lineas[i] == lineas[i].ToUpperInvariant())
                celda.Style.Font.Bold = true;
        }

        hoja.Column(1).Width = 110;
    }

    private static List<string> InstruccionesPlantilla() => new()
    {
        "PLANTILLA PARA CREAR PRODUCTOS NUEVOS",
        "Escribe un producto por fila en la hoja \"Productos\" y súbelo desde Inventario → Importar Excel.",
        "No cambies ni borres los encabezados de la fila 1.",
        "",
        "COLUMNAS (las de color fuerte son obligatorias)",
        "Nombre: único en todo el inventario (no importan las mayúsculas). Máximo 200 caracteres.",
        "Categoria: elige de la lista. Debe existir en Configuración → Categorías; si alguna no existe, el archivo completo se rechaza.",
        "Precio: mayor a 0. Es el valor final que paga el cliente.",
        "StockInicial: entero mayor a 0. No se crean productos con stock cero.",
        "StockMinimo: entero de 0 en adelante. Desde ese nivel se avisa de stock bajo.",
        "CodigoBarras: opcional y único. Si lo dejas vacío, el sistema genera uno interno.",
        "TarifaIva: opcional. Vacía = IVA general vigente; 0 = producto exento; o un porcentaje entre 0 y 100.",
        "Costo: opcional, 0 o más. Nunca se muestra al cliente ni en la factura.",
        "Proveedor: opcional. Elige de la lista (hoja \"Proveedores\") o déjalo vacío.",
        "",
        "Las hojas \"Categorias\" y \"Proveedores\" son solo de referencia para las listas desplegables: no las modifiques.",
    };

    private static List<string> InstruccionesExportacion() => new()
    {
        $"COPIA DEL INVENTARIO ACTUAL (generada el {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC)",
        "Contiene los productos activos con el mismo formato de la plantilla, más tres columnas al final.",
        "Puedes editar los productos existentes y agregar filas nuevas al final; luego súbelo desde Inventario → Importar Excel.",
        "Antes de guardar nada verás una vista previa. Si alguna fila tiene error, no se guarda NINGÚN cambio.",
        "No cambies ni borres los encabezados de la fila 1.",
        "",
        "PRODUCTOS EXISTENTES (filas con Id)",
        "Puedes cambiar: Nombre, Categoria, Precio, StockMinimo, CodigoBarras, TarifaIva, Costo y Proveedor.",
        "StockInicial no aplica: déjalo vacío. Para modificar el stock usa AjusteStock.",
        "AjusteStock: entero con signo. +5 suma 5 y -2 resta 2 sobre el stock real del momento de importar (no pisa ventas hechas después de exportar). El resultado no puede quedar negativo.",
        "StockActual: solo de lectura, sirve de referencia. Se ignora al importar.",
        "Id: identifica al producto. No lo modifiques ni lo borres; es lo que permite renombrar sin duplicar.",
        "",
        "PRODUCTOS NUEVOS (filas sin Id, al final)",
        "Llena las columnas como en la plantilla: StockInicial debe ser mayor a 0 y AjusteStock se deja vacío.",
        "",
        "Las hojas \"Categorias\" y \"Proveedores\" son solo de referencia para las listas desplegables: no las modifiques.",
    };

    private static byte[] Guardar(XLWorkbook libro)
    {
        using var flujo = new MemoryStream();
        libro.SaveAs(flujo);
        return flujo.ToArray();
    }
}
