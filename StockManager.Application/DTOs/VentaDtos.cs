namespace StockManager.Application.DTOs;

public record LineaVentaRequest(int ProductoId, int Cantidad);

public record RegistrarVentaRequest(
    int? ClienteId,
    string? NombreComprador,
    string? TelefonoComprador,
    string? EmailComprador,
    string MetodoPago,
    List<LineaVentaRequest> Lineas,
    decimal? MontoRecibido = null
);

public record DetalleVentaResponse(
    int Id,
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
    List<DetalleVentaResponse> Detalles,
    decimal? MontoRecibido = null,
    decimal? Cambio = null
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

public record CerrarFiadoRequest(string MetodoPago, decimal? MontoRecibido = null);

public record RegistrarAbonoRequest(decimal Monto, string MetodoPago);

public record AbonoResponse(
    int Id,
    int VentaId,
    decimal Monto,
    string MetodoPago,
    DateTime Fecha,
    int EmpleadoId
);

public record EditarCantidadLineaRequest(int Cantidad);
