namespace StockManager.Application.DTOs;

/// <summary>
/// DTO para la respuesta de un producto.
/// Incluye toda la información necesaria para listar y visualizar productos.
/// </summary>
public class ProductoResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public int CategoriaId { get; set; }
    public decimal Precio { get; set; }
    public int StockActual { get; set; }
    public int StockMinimo { get; set; }
    public string? CodigoBarras { get; set; }
    public bool Activo { get; set; }
}

/// <summary>
/// DTO para crear un nuevo producto.
/// CodigoBarras es opcional; si viene vacío, el sistema generará uno internamente.
/// </summary>
public class CrearProductoRequest
{
    public string Nombre { get; set; } = null!;
    public int CategoriaId { get; set; }
    public decimal Precio { get; set; }
    public int StockInicial { get; set; }
    public int StockMinimo { get; set; }
    public string? CodigoBarras { get; set; }
}

/// <summary>
/// DTO para actualizar un producto existente.
/// CodigoBarras es opcional; si viene vacío, se mantiene el actual.
/// </summary>
public class ActualizarProductoRequest
{
    public string Nombre { get; set; } = null!;
    public int CategoriaId { get; set; }
    public decimal Precio { get; set; }
    public int StockMinimo { get; set; }
    public string? CodigoBarras { get; set; }
}

/// <summary>
/// DTO para la respuesta de importación masiva desde Excel/CSV.
/// Incluye un resumen de la operación y lista de errores (si los hay).
/// </summary>
public class ImportarProductosResponse
{
    public int TotalFilas { get; set; }
    public int Creados { get; set; }
    public List<ErrorImportacion> Errores { get; set; } = new();
}

/// <summary>
/// Detalle de un error durante la importación.
/// </summary>
public class ErrorImportacion
{
    public int Fila { get; set; }
    public string Mensaje { get; set; } = null!;
}

/// <summary>
/// DTO para la respuesta de etiquetas pendientes.
/// </summary>
public class EtiquetaPendienteResponse
{
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = null!;
    public string CodigoBarras { get; set; } = null!;
}

/// <summary>
/// DTO para la respuesta de generación de etiquetas.
/// Incluye la imagen del código de barras en formato base64.
/// </summary>
public class EtiquetaGeneradaResponse
{
    public int ProductoId { get; set; }
    public string CodigoBarras { get; set; } = null!;
    public string ImagenBase64 { get; set; } = null!;
}
