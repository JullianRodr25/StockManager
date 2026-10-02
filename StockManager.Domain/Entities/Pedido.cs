namespace StockManager.Domain.Entities;

using StockManager.Domain.Exceptions;

/// <summary>
/// Entidad Pedido del dominio.
/// Representa un pedido realizado vía PWA para entrega a domicilio.
///
/// El estado avanza en una sola dirección — Pendiente → Confirmado → EnPreparacion →
/// EnCamino → Entregado — y solo mediante los métodos de esta clase, nunca fijando
/// Estado directamente, para que una transición inválida (saltarse un paso, o mover un
/// pedido ya Entregado) sea imposible de expresar. Cancelar() es la única salida desde
/// cualquier estado que no sea Entregado o el propio Cancelado.
/// </summary>
public class Pedido
{
    public int Id { get; private set; }
    public int ClienteId { get; private set; }
    public DateTime Fecha { get; private set; }
    public string Estado { get; private set; } = null!;  // Pendiente | Confirmado | EnPreparacion | EnCamino | Entregado | Cancelado
    public string Direccion { get; private set; } = null!;

    /// <summary>
    /// Coordenadas del pin que el cliente ubicó en el mapa al hacer el pedido (selector estilo
    /// Rappi en la PWA, ver componente MapaDireccion). Null en pedidos antiguos creados antes
    /// de que existiera el selector, o si el cliente completó el checkout sin confirmar un pin
    /// (no son obligatorias: Direccion en texto sigue siendo la fuente que usa el repartidor
    /// cuando no hay coordenadas).
    /// </summary>
    public double? Latitud { get; private set; }
    public double? Longitud { get; private set; }

    public decimal Total { get; private set; }

    /// <summary>
    /// Venta generada al marcar el pedido como Entregado (ver MarcarEntregado). Null hasta entonces.
    /// A partir de ahí el pedido queda enlazado a esa venta para efectos de historial y reportes.
    /// </summary>
    public int? VentaId { get; private set; }

    private Pedido() { }

    /// <summary>
    /// Crea un pedido. El Total se recibe ya calculado (suma de Cantidad * PrecioUnitario
    /// de sus líneas), igual que en Venta, para que quede congelado al precio del momento
    /// del pedido y no cambie si el producto sube o baja de precio después.
    /// </summary>
    public static Pedido Crear(int clienteId, string direccion, decimal total, double? latitud = null, double? longitud = null)
    {
        if (clienteId <= 0)
            throw new ArgumentException("ClienteId debe ser mayor a 0.", nameof(clienteId));

        if (string.IsNullOrWhiteSpace(direccion))
            throw new ArgumentException("La dirección no puede estar vacía.", nameof(direccion));

        if (total < 0)
            throw new ArgumentException("El total no puede ser negativo.", nameof(total));

        // Ambas coordenadas van juntas o ninguna — un solo valor suelto no ubica nada en el
        // mapa y probablemente indica un bug del lado del cliente (frontend) que las envía.
        if (latitud.HasValue != longitud.HasValue)
            throw new ArgumentException("Latitud y longitud deben enviarse juntas o ninguna de las dos.");

        if (latitud.HasValue && (latitud < -90 || latitud > 90))
            throw new ArgumentException("La latitud debe estar entre -90 y 90.", nameof(latitud));

        if (longitud.HasValue && (longitud < -180 || longitud > 180))
            throw new ArgumentException("La longitud debe estar entre -180 y 180.", nameof(longitud));

        return new Pedido
        {
            ClienteId = clienteId,
            Fecha = DateTime.UtcNow,
            Estado = "Pendiente",
            Direccion = direccion.Trim(),
            Latitud = latitud,
            Longitud = longitud,
            Total = total
        };
    }

    /// <summary>
    /// El admin confirma que el pedido se va a atender.
    /// </summary>
    public void Confirmar()
    {
        ExigirEstado("Pendiente");
        Estado = "Confirmado";
    }

    /// <summary>
    /// El pedido pasa a alistarse en bodega.
    /// </summary>
    public void IniciarPreparacion()
    {
        ExigirEstado("Confirmado");
        Estado = "EnPreparacion";
    }

    /// <summary>
    /// El pedido sale a domicilio.
    /// </summary>
    public void EnviarACamino()
    {
        ExigirEstado("EnPreparacion");
        Estado = "EnCamino";
    }

    /// <summary>
    /// Se entrega el pedido al cliente. Queda enlazado a la Venta que lo registra
    /// contablemente (generada por el servicio de aplicación junto con esta llamada).
    /// </summary>
    public void MarcarEntregado(int ventaId)
    {
        ExigirEstado("EnCamino");

        if (ventaId <= 0)
            throw new ArgumentException("VentaId debe ser mayor a 0.", nameof(ventaId));

        Estado = "Entregado";
        VentaId = ventaId;
    }

    /// <summary>
    /// Cancela el pedido. Permitido desde cualquier estado salvo Entregado o el propio Cancelado
    /// — una vez entregado, no hay pedido que cancelar; es una devolución, un flujo aparte.
    /// La reposición de stock de las líneas Disponible es responsabilidad del servicio de
    /// aplicación, igual que en Venta.CancelarCuenta.
    /// </summary>
    public void Cancelar()
    {
        if (Estado == "Entregado" || Estado == "Cancelado")
            throw new PedidoEstadoInvalidoException(Id, Estado, "Pendiente, Confirmado, EnPreparacion o EnCamino");

        Estado = "Cancelado";
    }

    private void ExigirEstado(string estadoEsperado)
    {
        if (Estado != estadoEsperado)
            throw new PedidoEstadoInvalidoException(Id, Estado, estadoEsperado);
    }
}
