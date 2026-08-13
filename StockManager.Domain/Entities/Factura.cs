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
    public string? Numero { get; private set; }
    public DateTime Fecha { get; private set; }
    public decimal Total { get; private set; }

    private Factura() { }

    public static Factura Crear(int? ventaId, int? pedidoId, decimal total)
    {
        // Exactamente una debe estar presente
        if ((!ventaId.HasValue && !pedidoId.HasValue) || (ventaId.HasValue && pedidoId.HasValue))
            throw new ArgumentException("Exactamente una de VentaId o PedidoId debe tener valor.");

        if (total <= 0)
            throw new ArgumentException("El total debe ser mayor a 0.", nameof(total));

        return new Factura
        {
            VentaId = ventaId,
            PedidoId = pedidoId,
            Fecha = DateTime.UtcNow,
            Total = total
        };
    }

    /// <summary>
    /// Genera el número de factura a partir del Id real asignado por la base de datos.
    /// Formato: "FAC-{Id:D8}" (8 dígitos con ceros a la izquierda).
    /// Solo se ejecuta si Numero es actualmente null.
    /// </summary>
    public void GenerarNumero()
    {
        if (Id <= 0)
            throw new InvalidOperationException("No se puede generar un número de factura sin un Id válido. La factura debe estar guardada en la base de datos.");

        if (Numero != null)
            return; // Ya tiene número, no se sobrescribe

        Numero = $"FAC-{Id:D8}";
    }
}
