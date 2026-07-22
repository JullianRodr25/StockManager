namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad DetallePedido del dominio.
/// Representa una línea de detalle dentro de un pedido.
/// El estado de cumplimiento es POR LÍNEA, no por pedido completo.
/// </summary>
public class DetallePedido
{
    public int Id { get; private set; }
    public int PedidoId { get; private set; }
    public int ProductoId { get; private set; }
    public int Cantidad { get; private set; }
    public string EstadoLinea { get; private set; } = null!;  // 'Disponible' | 'PorEncargo'

    private DetallePedido() { }

    public static DetallePedido Crear(int pedidoId, int productoId, int cantidad, string estadoLinea = "Disponible")
    {
        if (pedidoId <= 0)
            throw new ArgumentException("PedidoId debe ser mayor a 0.", nameof(pedidoId));

        if (productoId <= 0)
            throw new ArgumentException("ProductoId debe ser mayor a 0.", nameof(productoId));

        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a 0.", nameof(cantidad));

        if (estadoLinea != "Disponible" && estadoLinea != "PorEncargo")
            throw new ArgumentException("El estado de la línea debe ser 'Disponible' o 'PorEncargo'.", nameof(estadoLinea));

        return new DetallePedido
        {
            PedidoId = pedidoId,
            ProductoId = productoId,
            Cantidad = cantidad,
            EstadoLinea = estadoLinea
        };
    }

    public void CambiarEstadoLinea(string nuevoEstado)
    {
        if (nuevoEstado != "Disponible" && nuevoEstado != "PorEncargo")
            throw new ArgumentException("El estado de la línea debe ser 'Disponible' o 'PorEncargo'.", nameof(nuevoEstado));

        EstadoLinea = nuevoEstado;
    }
}
