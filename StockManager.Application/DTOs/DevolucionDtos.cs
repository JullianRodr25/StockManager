namespace StockManager.Application.DTOs;

/// <param name="DetalleVentaId">Línea de la venta original que se devuelve.</param>
/// <param name="Cantidad">Cuántas unidades; no puede superar lo vendido menos lo ya devuelto.</param>
/// <param name="ReintegrarAlStock">false cuando el producto vuelve dañado o inservible y no debe sumarse al inventario.</param>
public record LineaDevolucionRequest(int DetalleVentaId, int Cantidad, bool ReintegrarAlStock = true);

/// <summary>Producto que el cliente se lleva a cambio, al precio vigente.</summary>
public record LineaCambioRequest(int ProductoId, int Cantidad);

/// <param name="Motivo">Obligatorio: por qué se devuelve (queda en el historial).</param>
/// <param name="Devueltas">Al menos una línea.</param>
/// <param name="Cambio">Lo que el cliente se lleva a cambio. Null o vacío = devolución pura.</param>
/// <param name="MetodoPago">
/// Con qué se paga (si el cambio vale más) o se reembolsa (si vale menos) la diferencia:
/// Efectivo, Tarjeta o Transferencia. Se ignora cuando la diferencia es cero.
/// </param>
public record RegistrarDevolucionRequest(
    string Motivo,
    List<LineaDevolucionRequest> Devueltas,
    List<LineaCambioRequest>? Cambio = null,
    string? MetodoPago = null
);

public record DetalleDevolucionResponse(
    int Id,
    string Sentido,
    int? DetalleVentaId,
    int ProductoId,
    string NombreProducto,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal,
    bool ReintegradoAlStock
);

/// <param name="Diferencia">Positivo: el cliente pagó la diferencia. Negativo: se le reembolsó. Cero: sin movimiento de dinero.</param>
public record DevolucionResponse(
    int Id,
    int VentaId,
    int EmpleadoId,
    string? NombreEmpleado,
    DateTime Fecha,
    string Motivo,
    decimal TotalDevuelto,
    decimal TotalCambio,
    decimal Diferencia,
    string? MetodoPago,
    bool RequiereNotaCredito,
    List<DetalleDevolucionResponse> Detalles
);
