namespace StockManager.Application.DTOs;

/// <param name="MetodoPagoInmediato">
/// Si viene (Efectivo, Tarjeta o Transferencia) la cuenta se paga completa en el mismo momento
/// de registrarla y queda en estado Pagada. Si es null queda Pendiente, como siempre.
/// </param>
public record CrearCuentaPorPagarRequest(
    int ProveedorId,
    string Concepto,
    decimal MontoTotal,
    DateTime FechaVencimiento,
    string? MetodoPagoInmediato = null
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
