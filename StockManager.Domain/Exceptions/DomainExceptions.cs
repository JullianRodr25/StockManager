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
