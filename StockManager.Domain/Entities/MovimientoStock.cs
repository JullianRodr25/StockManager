namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad MovimientoStock del dominio.
/// Auditoría de todos los cambios de stock.
/// NUNCA se modifica StockActual directamente desde otro lugar — siempre vía MovimientoStock.
/// </summary>
public class MovimientoStock
{
    public int Id { get; private set; }
    public int ProductoId { get; private set; }
    public string Tipo { get; private set; } = null!;  // 'Entrada' | 'SalidaVenta' | 'SalidaPedido' | 'Ajuste'
    public int Cantidad { get; private set; }
    public DateTime Fecha { get; private set; }
    public string ReferenciaTipo { get; private set; } = null!;  // 'Venta' | 'Pedido' | 'Ajuste' | null
    public int? ReferenciaId { get; private set; }

    private MovimientoStock() { }

    public static MovimientoStock Crear(
        int productoId,
        string tipo,
        int cantidad,
        string referenciaTipo,
        int? referenciaId = null)
    {
        if (productoId <= 0)
            throw new ArgumentException("ProductoId debe ser mayor a 0.", nameof(productoId));

        var tipos = new[] { "Entrada", "SalidaVenta", "SalidaPedido", "Ajuste" };
        if (!tipos.Contains(tipo))
            throw new ArgumentException($"El tipo '{tipo}' no es válido.", nameof(tipo));

        if (cantidad <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a 0.", nameof(cantidad));

        if (string.IsNullOrWhiteSpace(referenciaTipo))
            throw new ArgumentException("El tipo de referencia no puede estar vacío.", nameof(referenciaTipo));

        return new MovimientoStock
        {
            ProductoId = productoId,
            Tipo = tipo,
            Cantidad = cantidad,
            Fecha = DateTime.UtcNow,
            ReferenciaTipo = referenciaTipo,
            ReferenciaId = referenciaId
        };
    }
}
