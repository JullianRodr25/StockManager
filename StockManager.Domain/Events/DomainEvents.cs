namespace StockManager.Domain.Events;

/// <summary>
/// Base para todos los eventos de dominio.
/// Los eventos se disparan cuando ocurren cambios importantes en el dominio.
/// </summary>
public abstract class DomainEvent
{
    public DateTime OcurridoEn { get; } = DateTime.UtcNow;
}

/// <summary>
/// Evento disparado cuando se vende una cantidad de un producto.
/// </summary>
public class ProductoVendidoEvent : DomainEvent
{
    public int ProductoId { get; }
    public int Cantidad { get; }
    public string TipoReferencia { get; }  // "Venta" o "Pedido"
    public int ReferenciaId { get; }

    public ProductoVendidoEvent(int productoId, int cantidad, string tipoReferencia, int referenciaId)
    {
        ProductoId = productoId;
        Cantidad = cantidad;
        TipoReferencia = tipoReferencia;
        ReferenciaId = referenciaId;
    }
}

/// <summary>
/// Evento disparado cuando se repone stock de un producto.
/// </summary>
public class ProductoRepuestoEvent : DomainEvent
{
    public int ProductoId { get; }
    public int CantidadRepuesta { get; }
    public int NuevoStock { get; }

    public ProductoRepuestoEvent(int productoId, int cantidadRepuesta, int nuevoStock)
    {
        ProductoId = productoId;
        CantidadRepuesta = cantidadRepuesta;
        NuevoStock = nuevoStock;
    }
}
