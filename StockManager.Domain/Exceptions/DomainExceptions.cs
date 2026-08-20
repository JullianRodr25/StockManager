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
