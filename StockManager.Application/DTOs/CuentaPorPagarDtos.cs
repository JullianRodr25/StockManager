namespace StockManager.Application.DTOs;

public record CrearCuentaPorPagarRequest(
    int ProveedorId,
    string Concepto,
    decimal MontoTotal,
    DateTime FechaVencimiento
);

public record RegistrarAbonoCuentaPorPagarRequest(decimal Monto, string MetodoPago);

public record AbonoCuentaPorPagarResponse(
    int Id,
    decimal Monto,
    string MetodoPago,
    DateTime Fecha,
    int EmpleadoId
);

public record CuentaPorPagarResponse(
    int Id,
    int ProveedorId,
    string ProveedorNombre,
    string Concepto,
    decimal MontoTotal,
    decimal SaldoPendiente,
    DateTime FechaCompra,
    DateTime FechaVencimiento,
    string Estado,
    bool Vencida,
    List<AbonoCuentaPorPagarResponse> Abonos
);

public record CuentaPorPagarResumenResponse(
    int Id,
    int ProveedorId,
    string ProveedorNombre,
    string Concepto,
    decimal MontoTotal,
    decimal SaldoPendiente,
    DateTime FechaVencimiento,
    string Estado,
    bool Vencida
);
