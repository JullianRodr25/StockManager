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
    string? MetodoPago,
    int EmpleadoId,
    DateTime Fecha,
    string Estado,
    decimal Total,
    string NumeroFactura,
    List<DetalleVentaResponse> Detalles
);

public record VentaResumenResponse(
    int Id,
    string? NombreComprador,
    int? ClienteId,
    DateTime Fecha,
    string Estado,
    decimal Total,
    string? MetodoPago,
    string NumeroFactura
);

public record AbrirFiadoRequest(int ClienteId);

public record CerrarFiadoRequest(string MetodoPago);
