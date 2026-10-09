using System.ComponentModel.DataAnnotations;

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
    public bool AplicaIva { get; set; }
    public decimal TarifaIva { get; set; }
    public decimal Costo { get; set; }
    public string? CodigoBarras { get; set; }
    public bool Activo { get; set; }
    public int? ProveedorId { get; set; }

    /// <summary>
    /// Galería de fotos del producto, ordenadas para el carrusel (ver ProductoFoto). Lista
    /// vacía si el producto todavía no tiene fotos — nunca null, para que el frontend no tenga
    /// que chequear nulidad antes de iterar.
    /// </summary>
    public List<ProductoFotoResponse> Fotos { get; set; } = new();
}

/// <summary>
/// DTO de una foto individual de la galería de un producto.
/// </summary>
public record ProductoFotoResponse(
    int Id,
    string Url,
    int Orden
);

public record ProductoCatalogoResponse(
    int Id,
    string Nombre,
    string CategoriaNombre,
    decimal Precio,
    bool Disponible,
    IReadOnlyList<ProductoFotoResponse> Fotos,
    decimal? CalificacionPromedio,
    int TotalResenas
);

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
    // Si el producto causa IVA. TarifaIva solo se usa (y se exige con sentido) cuando esto es
    // true; si es false, el backend siempre guarda TarifaIva = 0 sin importar lo que venga acá.
    public bool AplicaIva { get; set; } = true;
    [Range(0, 100)]
    public decimal? TarifaIva { get; set; }
    // Costo de adquisición, para métricas de rentabilidad. Nunca participa en el cálculo de
    // una venta ni se muestra al cliente.
    public decimal Costo { get; set; }
    public string? CodigoBarras { get; set; }
    public int? ProveedorId { get; set; }
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
    public bool AplicaIva { get; set; } = true;
    [Range(0, 100)]
    public decimal? TarifaIva { get; set; }
    public decimal Costo { get; set; }
    public string? CodigoBarras { get; set; }
    public int? ProveedorId { get; set; }
}

/// <summary>
/// DTO para ajustar manualmente el stock de un producto (ej. llegó mercancía, corrección de
/// un conteo físico). Delta puede ser positivo (suma) o negativo (resta); el backend valida
/// que el resultado no quede negativo.
/// </summary>
public class AjustarStockRequest
{
    public int Delta { get; set; }
}

/// <summary>
/// DTO para la respuesta de importación masiva desde Excel (vista previa o aplicación).
/// La importación es "todo o nada": si hay errores no se aplica ningún cambio.
/// </summary>
public class ImportarProductosResponse
{
    public int TotalFilas { get; set; }
    public int Creados { get; set; }
    public int Modificados { get; set; }
    public int SinCambios { get; set; }

    /// <summary>True solo si los cambios se guardaron en la base de datos (false en vista previa o con errores).</summary>
    public bool Aplicado { get; set; }
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
