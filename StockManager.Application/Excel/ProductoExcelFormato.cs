namespace StockManager.Application.Excel;

/// <summary>
/// Contrato del Excel de productos: nombres de hojas y posición de cada columna.
///
/// Vive en un solo lugar porque lo comparten quien GENERA el archivo (plantilla y exportación) y
/// quien lo LEE (la importación). Si cada lado tuviera sus propios "números mágicos" de columna,
/// bastaría con mover una para que el archivo exportado ya no se pudiera volver a importar.
///
/// Las columnas 1 a 8 conservan el orden histórico de la importación (Nombre, Categoría, Precio,
/// StockInicial, StockMinimo, CodigoBarras, TarifaIva, Costo) para que los Excel que ya usaba el
/// negocio sigan funcionando. Todo lo nuevo se agrega al final.
/// </summary>
public static class ProductoExcelFormato
{
    public const string HojaProductos = "Productos";
    public const string HojaInstrucciones = "Instrucciones";

    // Sin tildes ni espacios a propósito: son los nombres que usan las listas desplegables
    // (validación de datos) para apuntar a estas hojas, y así no dependen de comillas ni de codificación.
    public const string HojaCategorias = "Categorias";
    public const string HojaProveedores = "Proveedores";

    public const int FilaEncabezado = 1;
    public const int PrimeraFilaDatos = 2;

    // Cantidad mínima de filas con reglas de validación (aunque el archivo traiga menos datos),
    // para que quien agregue productos nuevos al final también tenga las listas y los límites.
    public const int FilasMinimasConValidacion = 2000;

    public const int ColNombre = 1;
    public const int ColCategoria = 2;
    public const int ColPrecio = 3;
    public const int ColStockInicial = 4;
    public const int ColStockMinimo = 5;
    public const int ColCodigoBarras = 6;
    public const int ColTarifaIva = 7;
    public const int ColCosto = 8;
    public const int ColProveedor = 9;

    // Solo en la exportación (edición de productos existentes).
    public const int ColStockActual = 10;
    public const int ColAjusteStock = 11;
    public const int ColId = 12;

    /// <summary>Columnas de la plantilla para productos nuevos (sin las de edición).</summary>
    public const int TotalColumnasPlantilla = ColProveedor;

    /// <summary>Columnas del archivo exportado (incluye las de edición).</summary>
    public const int TotalColumnasExportacion = ColId;

    // Los encabezados conservan los nombres que el negocio ya conocía (sin espacios) para no
    // desorientar a quien ya usaba la plantilla anterior.
    public static readonly string[] Encabezados =
    {
        "Nombre",
        "Categoria",
        "Precio",
        "StockInicial",
        "StockMinimo",
        "CodigoBarras",
        "TarifaIva",
        "Costo",
        "Proveedor",
        "StockActual",
        "AjusteStock",
        "Id",
    };

    /// <summary>Columnas que no se pueden dejar vacías al crear un producto nuevo.</summary>
    public static readonly int[] ColumnasObligatorias =
    {
        ColNombre,
        ColCategoria,
        ColPrecio,
        ColStockInicial,
        ColStockMinimo,
    };

    public const string TipoContenidoExcel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
