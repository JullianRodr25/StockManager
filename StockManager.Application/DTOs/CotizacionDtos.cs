using System.ComponentModel.DataAnnotations;

namespace StockManager.Application.DTOs;

public record LineaCotizacionRequest(
    int ProductoId,
    int Cantidad,
    // null = precio de lista actual. Un precio distinto al de lista (descuento) solo lo puede
    // fijar un Admin; al editar, un Caja puede conservar el precio ya cotizado de la línea.
    decimal? PrecioUnitario = null);

public class GuardarCotizacionRequest
{
    public int? ClienteId { get; set; }

    [MaxLength(200)]
    public string? NombreCliente { get; set; }

    [MaxLength(50)]
    public string? TelefonoCliente { get; set; }

    [MaxLength(200)]
    public string? EmailCliente { get; set; }

    /// <summary>Último día de validez (inclusive), en calendario de Colombia.</summary>
    public DateOnly FechaVencimiento { get; set; }

    [MaxLength(500)]
    public string? Notas { get; set; }

    public List<LineaCotizacionRequest> Lineas { get; set; } = new();
}

public record LineaCotizacionResponse(
    int ProductoId,
    string ProductoNombre,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal,
    // Precio de lista de hoy, para que la pantalla muestre si cambió desde que se cotizó.
    decimal PrecioActual,
    int StockDisponible);

public record CotizacionResponse(
    int Id,
    string Numero,
    int EmpleadoId,
    string EmpleadoNombre,
    int? ClienteId,
    string? NombreCliente,
    string? TelefonoCliente,
    string? EmailCliente,
    DateTime Fecha,
    DateOnly FechaVencimiento,
    /// <summary>Vigente, Vencida, Convertida o Anulada.</summary>
    string Estado,
    string? Notas,
    decimal Total,
    int? VentaId,
    string? NumeroFactura,
    DateTime? FechaConversion,
    List<LineaCotizacionResponse> Lineas);

public record CotizacionResumenResponse(
    int Id,
    string Numero,
    string? NombreCliente,
    string EmpleadoNombre,
    DateTime Fecha,
    DateOnly FechaVencimiento,
    string Estado,
    decimal Total,
    int CantidadLineas,
    int? VentaId);

/// <summary>
/// Datos de cobro al convertir una cotización en venta. Las líneas, el cliente y los precios
/// salen de la cotización: la pantalla no los reenvía, así no pueden alterarse en el camino.
/// </summary>
public class ConvertirCotizacionRequest
{
    [Required]
    public string MetodoPago { get; set; } = null!;

    public decimal? MontoRecibido { get; set; }
    public List<DetallePagoRequest>? DetallesPago { get; set; }

    public bool RequiereFacturaElectronica { get; set; }
    public string? TipoDocumentoFiscal { get; set; }
    public string? NumeroDocumentoFiscal { get; set; }
    public string? RazonSocialFiscal { get; set; }
    public string? DireccionFiscal { get; set; }
    public string? EmailFacturacion { get; set; }
}
