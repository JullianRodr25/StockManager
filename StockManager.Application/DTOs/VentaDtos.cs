namespace StockManager.Application.DTOs;

public record LineaVentaRequest(int ProductoId, int Cantidad);

public record RegistrarVentaRequest(
    int? ClienteId,
    string? NombreComprador,
    string? TelefonoComprador,
    string? EmailComprador,
    string MetodoPago,
    List<LineaVentaRequest> Lineas
);

public record DetalleVentaResponse(
    int ProductoId,
    string ProductoNombre,
    int Cantidad,
    decimal PrecioUnitario,
    decimal SubtotalSinIva,
    decimal Iva,
    decimal SubtotalConIva
);

public record VentaResponse(
    int Id,
    int? ClienteId,
    string? NombreComprador,
    string? TelefonoComprador,
    string? EmailComprador,
    string MetodoPago,
    int EmpleadoId,
    DateTime Fecha,
    string Estado,
    decimal Total,
    List<DetalleVentaResponse> Detalles
);
