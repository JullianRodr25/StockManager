namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad Factura del dominio.
/// Representa una factura generada desde una Venta o un Pedido.
/// Exactamente una de las dos debe estar presente (validación via constraint).
/// </summary>
public class Factura
{
    public int Id { get; private set; }
    public int? VentaId { get; private set; }
    public int? PedidoId { get; private set; }
    public string Numero { get; private set; } = null!;
    public DateTime Fecha { get; private set; }
    public decimal Total { get; private set; }

    private Factura() { }

    public static Factura Crear(int? ventaId, int? pedidoId, string numero, decimal total)
    {
        // Exactamente una debe estar presente
        if ((!ventaId.HasValue && !pedidoId.HasValue) || (ventaId.HasValue && pedidoId.HasValue))
            throw new ArgumentException("Exactamente una de VentaId o PedidoId debe tener valor.");

        if (string.IsNullOrWhiteSpace(numero))
            throw new ArgumentException("El número de factura no puede estar vacío.", nameof(numero));

        if (total <= 0)
            throw new ArgumentException("El total debe ser mayor a 0.", nameof(total));

        return new Factura
        {
            VentaId = ventaId,
            PedidoId = pedidoId,
            Numero = numero.Trim(),
            Fecha = DateTime.UtcNow,
            Total = total
        };
    }
}
