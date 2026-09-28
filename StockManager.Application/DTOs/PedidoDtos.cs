namespace StockManager.Application.DTOs;

public record LineaPedidoRequest(int ProductoId, int Cantidad);

/// <summary>
/// Petición de checkout desde la PWA. El ClienteId no viaja en el body: se toma del
/// token del cliente autenticado, igual que EmpleadoId se toma del token en Ventas.
/// </summary>
public record CrearPedidoRequest(string Direccion, List<LineaPedidoRequest> Lineas);

public record MarcarEntregadoRequest(string MetodoPago);

public record DetallePedidoResponse(
    int Id,
    int ProductoId,
    string ProductoNombre,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal,
    string EstadoLinea
);

public record PedidoResponse(
    int Id,
    int ClienteId,
    string ClienteNombre,
    string Direccion,
    DateTime Fecha,
    string Estado,
    decimal Total,
    int? VentaId,
    List<DetallePedidoResponse> Detalles
);

public record PedidoResumenResponse(
    int Id,
    string ClienteNombre,
    DateTime Fecha,
    string Estado,
    decimal Total,
    bool TieneLineasPorEncargo
);
