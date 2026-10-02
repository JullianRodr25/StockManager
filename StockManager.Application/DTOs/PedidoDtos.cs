namespace StockManager.Application.DTOs;

public record LineaPedidoRequest(int ProductoId, int Cantidad);

/// <summary>
/// Petición de checkout desde la PWA. El ClienteId no viaja en el body: se toma del
/// token del cliente autenticado, igual que EmpleadoId se toma del token en Ventas.
/// Latitud/Longitud son opcionales (selector de mapa estilo Rappi en Checkout): si el cliente
/// no confirmó un pin, viajan null y el pedido queda solo con la dirección en texto.
/// </summary>
public record CrearPedidoRequest(string Direccion, List<LineaPedidoRequest> Lineas, double? Latitud = null, double? Longitud = null);

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
    double? Latitud,
    double? Longitud,
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
