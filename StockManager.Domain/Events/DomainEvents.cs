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

/// <summary>
/// Evento disparado cuando un Pedido cambia de estado (incluye su creación, que queda
/// en estado "Pendiente"). Lo consume el despachador de notificaciones para avisar por
/// WhatsApp al cliente (y, si el pedido es nuevo, también a la tienda).
/// </summary>
public class PedidoEstadoCambiadoEvent : DomainEvent
{
    public int PedidoId { get; }
    public string NuevoEstado { get; }

    public PedidoEstadoCambiadoEvent(int pedidoId, string nuevoEstado)
    {
        PedidoId = pedidoId;
        NuevoEstado = nuevoEstado;
    }
}

/// <summary>
/// Evento disparado cuando se genera una Factura. Una Factura solo se crea una vez el
/// pago de la Venta asociada ya está completo (venta de mostrador pagada al instante,
/// cuenta fiada saldada, o pedido a domicilio entregado), así que este evento es el
/// punto único donde se dispara el envío de la factura por WhatsApp.
/// </summary>
public class FacturaGeneradaEvent : DomainEvent
{
    public int FacturaId { get; }

    public FacturaGeneradaEvent(int facturaId)
    {
        FacturaId = facturaId;
    }
}

/// <summary>
/// Evento disparado por el chequeo periódico de cuentas por pagar (no por una transacción
/// de negocio puntual) cuando una CuentaPorPagar entra en su ventana de aviso de
/// vencimiento (o ya está vencida) y no se le ha avisado al admin todavía hoy.
/// </summary>
public class CuentaPorPagarProximaAVencerEvent : DomainEvent
{
    public int CuentaPorPagarId { get; }

    public CuentaPorPagarProximaAVencerEvent(int cuentaPorPagarId)
    {
        CuentaPorPagarId = cuentaPorPagarId;
    }
}
