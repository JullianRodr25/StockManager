namespace StockManager.Domain.Exceptions;

/// <summary>
/// Excepción base para todas las excepciones de dominio.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Excepción lanzada cuando hay un error de stock.
/// </summary>
public class StockInsuficienteException : DomainException
{
    public int StockActual { get; }
    public int CantidadSolicitada { get; }

    public StockInsuficienteException(int stockActual, int cantidadSolicitada)
        : base($"Stock insuficiente. Disponible: {stockActual}, Solicitado: {cantidadSolicitada}")
    {
        StockActual = stockActual;
        CantidadSolicitada = cantidadSolicitada;
    }
}

/// <summary>
/// Excepción lanzada cuando se intenta operar sobre un producto inactivo.
/// </summary>
public class ProductoInactivoException : DomainException
{
    public int ProductoId { get; }

    public ProductoInactivoException(int productoId, string nombreProducto)
        : base($"El producto '{nombreProducto}' (ID: {productoId}) está inactivo y no puede ser vendido.")
    {
        ProductoId = productoId;
    }
}

/// <summary>
/// Excepción lanzada en violaciones de concurrencia optimista.
/// </summary>
public class ConcurrencyException : DomainException
{
    public ConcurrencyException(string message) : base(message) { }
}

/// <summary>
/// Excepción lanzada cuando se intenta registrar un usuario con un NumeroIdentificacion duplicado.
/// </summary>
public class UsuarioDuplicadoPorIdentificacionException : DomainException
{
    public string NumeroIdentificacion { get; }

    public UsuarioDuplicadoPorIdentificacionException(string numeroIdentificacion)
        : base($"Ya existe un usuario registrado con el número de identificación: {numeroIdentificacion}")
    {
        NumeroIdentificacion = numeroIdentificacion;
    }
}

/// <summary>
/// Excepción lanzada cuando se intenta registrar un usuario con un Email duplicado.
/// </summary>
public class UsuarioDuplicadoPorEmailException : DomainException
{
    public string Email { get; }

    public UsuarioDuplicadoPorEmailException(string email)
        : base($"Ya existe un usuario registrado con el email: {email}")
    {
        Email = email;
    }
}

/// <summary>
/// Excepción lanzada cuando se intenta crear una categoría con un nombre que ya existe.
/// </summary>
public class CategoriaDuplicadaException : DomainException
{
    public string NombreCategoria { get; }

    public CategoriaDuplicadaException(string nombreCategoria)
        : base($"Ya existe una categoría con el nombre: '{nombreCategoria}'")
    {
        NombreCategoria = nombreCategoria;
    }
}

/// <summary>
/// Excepción lanzada cuando se intenta crear un producto con un nombre que ya existe.
/// </summary>
public class ProductoDuplicadoException : DomainException
{
    public string Nombre { get; }

    public ProductoDuplicadoException(string nombre)
        : base($"Ya existe un producto con el nombre '{nombre}'.")
    {
        Nombre = nombre;
    }
}

/// <summary>
/// Excepción lanzada cuando no se encuentra un producto con el ID especificado.
/// </summary>
public class ProductoNoEncontradoException : DomainException
{
    public int ProductoId { get; }

    public ProductoNoEncontradoException(int productoId)
        : base($"No se encontró el producto con ID {productoId}.")
    {
        ProductoId = productoId;
    }
}

/// <summary>
/// Excepción lanzada cuando no se encuentra una venta con el ID especificado.
/// </summary>
public class VentaNoEncontradaException : DomainException
{
    public int VentaId { get; }

    public VentaNoEncontradaException(int ventaId)
        : base($"No se encontró la venta con ID {ventaId}.")
    {
        VentaId = ventaId;
    }
}

/// <summary>
/// Excepción lanzada cuando se intenta una operación sobre una venta que no está en el estado requerido.
/// </summary>
public class VentaEstadoInvalidoException : DomainException
{
    public int VentaId { get; }
    public string EstadoActual { get; }
    public string EstadoEsperado { get; }

    public VentaEstadoInvalidoException(int ventaId, string estadoActual, string estadoEsperado)
        : base($"La venta {ventaId} está en estado '{estadoActual}', se esperaba '{estadoEsperado}'.")
    {
        VentaId = ventaId;
        EstadoActual = estadoActual;
        EstadoEsperado = estadoEsperado;
    }
}

/// <summary>
/// Excepción lanzada cuando un cliente ya tiene una cuenta fiada (venta Pendiente) abierta.
/// </summary>
public class CuentaFiadoAbiertaException : DomainException
{
    public int ClienteId { get; }

    public CuentaFiadoAbiertaException(int clienteId)
        : base($"El cliente con ID {clienteId} ya tiene una cuenta fiada abierta.")
    {
        ClienteId = clienteId;
    }
}

/// <summary>
/// Excepción lanzada al intentar cancelar una cuenta fiada que ya tiene abonos registrados.
/// </summary>
public class CuentaConAbonosException : DomainException
{
    public int VentaId { get; }

    public CuentaConAbonosException(int ventaId)
        : base($"La cuenta {ventaId} no se puede cancelar porque ya tiene abonos registrados.")
    {
        VentaId = ventaId;
    }
}

/// <summary>
/// Excepción lanzada al intentar dejar una cuenta fiada sin productos (última línea) o al
/// intentar reducir su Total por debajo de lo ya abonado.
/// </summary>
public class OperacionInvalidaCuentaFiadaException : DomainException
{
    public OperacionInvalidaCuentaFiadaException(string message) : base(message) { }
}

/// <summary>
/// Excepción lanzada cuando no se encuentra un pedido con el ID especificado.
/// </summary>
public class PedidoNoEncontradoException : DomainException
{
    public int PedidoId { get; }

    public PedidoNoEncontradoException(int pedidoId)
        : base($"No se encontró el pedido con ID {pedidoId}.")
    {
        PedidoId = pedidoId;
    }
}

/// <summary>
/// Excepción lanzada cuando se intenta una operación sobre un pedido que no está en el
/// estado requerido (por ejemplo, saltarse un paso del flujo Pendiente → Confirmado →
/// EnPreparacion → EnCamino → Entregado).
/// </summary>
public class PedidoEstadoInvalidoException : DomainException
{
    public int PedidoId { get; }
    public string EstadoActual { get; }
    public string EstadoEsperado { get; }

    public PedidoEstadoInvalidoException(int pedidoId, string estadoActual, string estadoEsperado)
        : base($"El pedido {pedidoId} está en estado '{estadoActual}', se esperaba '{estadoEsperado}'.")
    {
        PedidoId = pedidoId;
        EstadoActual = estadoActual;
        EstadoEsperado = estadoEsperado;
    }
}

/// <summary>
/// Excepción lanzada al intentar una operación sobre un pedido que su estado actual no
/// permite por reglas de negocio propias del flujo (por ejemplo, entregarlo mientras aún
/// tiene líneas "Por encargo" sin resolver).
/// </summary>
public class OperacionInvalidaPedidoException : DomainException
{
    public OperacionInvalidaPedidoException(string message) : base(message) { }
}

/// <summary>
/// Excepción lanzada cuando no se encuentra un proveedor con el ID especificado.
/// </summary>
public class ProveedorNoEncontradoException : DomainException
{
    public int ProveedorId { get; }

    public ProveedorNoEncontradoException(int proveedorId)
        : base($"No se encontró el proveedor con ID {proveedorId}.")
    {
        ProveedorId = proveedorId;
    }
}

/// <summary>
/// Excepción lanzada cuando no se encuentra una cuenta por pagar con el ID especificado.
/// </summary>
public class CuentaPorPagarNoEncontradaException : DomainException
{
    public int CuentaPorPagarId { get; }

    public CuentaPorPagarNoEncontradaException(int cuentaPorPagarId)
        : base($"No se encontró la cuenta por pagar con ID {cuentaPorPagarId}.")
    {
        CuentaPorPagarId = cuentaPorPagarId;
    }
}

/// <summary>
/// Excepción lanzada cuando se intenta una operación sobre una cuenta por pagar que no
/// está en el estado requerido (ej. pagar o cancelar una que ya no está Pendiente).
/// </summary>
public class CuentaPorPagarEstadoInvalidoException : DomainException
{
    public int CuentaPorPagarId { get; }
    public string EstadoActual { get; }
    public string EstadoEsperado { get; }

    public CuentaPorPagarEstadoInvalidoException(int cuentaPorPagarId, string estadoActual, string estadoEsperado)
        : base($"La cuenta por pagar {cuentaPorPagarId} está en estado '{estadoActual}', se esperaba '{estadoEsperado}'.")
    {
        CuentaPorPagarId = cuentaPorPagarId;
        EstadoActual = estadoActual;
        EstadoEsperado = estadoEsperado;
    }
}

/// <summary>
/// Excepción lanzada al intentar cancelar una cuenta por pagar que ya tiene abonos
/// registrados, o dejar su saldo negativo con un abono mayor al pendiente.
/// </summary>
public class OperacionInvalidaCuentaPorPagarException : DomainException
{
    public OperacionInvalidaCuentaPorPagarException(string message) : base(message) { }
}

/// <summary>
/// Excepción lanzada cuando no se encuentra un cliente con el ID especificado.
/// </summary>
public class ClienteNoEncontradoException : DomainException
{
    public int ClienteId { get; }

    public ClienteNoEncontradoException(int clienteId)
        : base($"No se encontró el cliente con ID {clienteId}.")
    {
        ClienteId = clienteId;
    }
}

/// <summary>
/// Excepción lanzada al intentar desactivar un cliente que todavía tiene Pedidos en un
/// estado activo (Pendiente, Confirmado, EnPreparacion o EnCamino). Desactivarlo rompería
/// la posibilidad de completar/entregar ese pedido en curso.
/// </summary>
public class ClienteConPedidosActivosException : DomainException
{
    public int ClienteId { get; }

    public ClienteConPedidosActivosException(int clienteId)
        : base($"El cliente con ID {clienteId} tiene pedidos activos y no puede desactivarse.")
    {
        ClienteId = clienteId;
    }
}

/// <summary>
/// Excepción lanzada cuando un cliente intenta cambiar su propia contraseña (autenticado)
/// pero la contraseña actual que confirmó no coincide con la almacenada.
/// </summary>
public class ContrasenaActualIncorrectaException : DomainException
{
    public ContrasenaActualIncorrectaException()
        : base("La contraseña actual no es correcta.")
    {
    }
}

/// <summary>
/// Excepción lanzada cuando no se encuentra una foto de producto con el ID especificado (o no
/// pertenece al producto indicado).
/// </summary>
public class ProductoFotoNoEncontradaException : DomainException
{
    public int ProductoId { get; }
    public int FotoId { get; }

    public ProductoFotoNoEncontradaException(int productoId, int fotoId)
        : base($"No se encontró la foto {fotoId} para el producto {productoId}.")
    {
        ProductoId = productoId;
        FotoId = fotoId;
    }
}

/// <summary>
/// Excepción lanzada al intentar agregar una foto a un producto que ya alcanzó el máximo
/// permitido (ver Producto.MaxFotos).
/// </summary>
public class LimiteFotosProductoExcedidoException : DomainException
{
    public int ProductoId { get; }
    public int Maximo { get; }

    public LimiteFotosProductoExcedidoException(int productoId, int maximo)
        : base($"El producto {productoId} ya tiene el máximo de {maximo} fotos permitidas.")
    {
        ProductoId = productoId;
        Maximo = maximo;
    }
}

/// <summary>
/// Excepción lanzada cuando no se encuentra una notificación interna con el ID especificado.
/// </summary>
public class NotificacionInternaNoEncontradaException : DomainException
{
    public int NotificacionId { get; }

    public NotificacionInternaNoEncontradaException(int notificacionId)
        : base($"No se encontró la notificación con ID {notificacionId}.")
    {
        NotificacionId = notificacionId;
    }
}

/// <summary>
/// Excepción lanzada cuando no se encuentra una reseña con el ID especificado, o existe pero
/// no pertenece al cliente que intenta editarla/borrarla — se usa el mismo mensaje genérico
/// para los dos casos a propósito, para no filtrarle a un cliente si una reseña ajena existe.
/// </summary>
public class ResenaNoEncontradaException : DomainException
{
    public int ResenaId { get; }

    public ResenaNoEncontradaException(int resenaId)
        : base("La reseña no existe.")
    {
        ResenaId = resenaId;
    }
}

/// <summary>
/// Excepción lanzada al intentar crear una reseña para un producto que el cliente ya reseñó
/// (regla de negocio: una reseña por cliente por producto, ver ResenaProductoConfiguration).
/// </summary>
public class ResenaDuplicadaException : DomainException
{
    public int ProductoId { get; }
    public int ClienteId { get; }

    public ResenaDuplicadaException(int productoId, int clienteId)
        : base("Ya dejaste una reseña para este producto. Edítala en vez de crear una nueva.")
    {
        ProductoId = productoId;
        ClienteId = clienteId;
    }
}
