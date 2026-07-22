namespace StockManager.Domain.Entities;

using StockManager.Domain.Exceptions;

/// <summary>
/// Entidad Producto del dominio.
/// Representa un artículo del inventario con control de stock.
/// 
/// Invariantes de negocio:
/// - StockActual nunca puede ser negativo
/// - StockMinimo debe ser >= 0
/// - StockActual no se modifica directamente; solo vía métodos Vender/Reponer
/// - Precio debe ser > 0
/// - RowVersion se usa para concurrencia optimista (crítico en operaciones simultáneas de venta)
/// </summary>
public class Producto
{
    public int Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public int CategoriaId { get; private set; }
    public decimal Precio { get; private set; }
    public int StockActual { get; private set; }
    public int StockMinimo { get; private set; }
    public bool Activo { get; private set; }

    // Concurrencia optimista — EF Core maneja automáticamente este campo
    public byte[]? RowVersion { get; set; }

    // Constructor privado para EF Core
    private Producto() { }

    /// <summary>
    /// Factory method para crear un nuevo Producto con validaciones completas.
    /// </summary>
    public static Producto Crear(
        string nombre,
        int categoriaId,
        decimal precio,
        int stockActual,
        int stockMinimo)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del producto no puede estar vacío.", nameof(nombre));

        if (nombre.Length > 200)
            throw new ArgumentException("El nombre del producto no puede exceder 200 caracteres.", nameof(nombre));

        if (categoriaId <= 0)
            throw new ArgumentException("CategoriaId debe ser mayor a 0.", nameof(categoriaId));

        if (precio <= 0)
            throw new ArgumentException("El precio debe ser mayor a 0.", nameof(precio));

        if (stockActual < 0)
            throw new ArgumentException("El stock actual no puede ser negativo.", nameof(stockActual));

        if (stockMinimo < 0)
            throw new ArgumentException("El stock mínimo no puede ser negativo.", nameof(stockMinimo));

        return new Producto
        {
            Nombre = nombre.Trim(),
            CategoriaId = categoriaId,
            Precio = precio,
            StockActual = stockActual,
            StockMinimo = stockMinimo,
            Activo = true
        };
    }

    /// <summary>
    /// Decrementa el stock al vender un producto.
    /// Lanza excepción si la operación violaría la invariante (stock negativo).
    /// </summary>
    /// <param name="cantidad">Cantidad a vender (debe ser > 0)</param>
    /// <returns>El nuevo stock tras la venta</returns>
    /// <exception cref="ProductoInactivoException">Si el producto está inactivo</exception>
    /// <exception cref="StockInsuficienteException">Si el stock resultante sería negativo</exception>
    public int Vender(int cantidad)
    {
        if (!Activo)
            throw new ProductoInactivoException(Id, Nombre);

        if (cantidad <= 0)
            throw new ArgumentException("La cantidad a vender debe ser mayor a 0.", nameof(cantidad));

        if (StockActual - cantidad < 0)
            throw new StockInsuficienteException(StockActual, cantidad);

        StockActual -= cantidad;
        return StockActual;
    }

    /// <summary>
    /// Incrementa el stock al reponer inventario.
    /// </summary>
    /// <param name="cantidad">Cantidad a reponer (debe ser > 0)</param>
    /// <returns>El nuevo stock tras la reposición</returns>
    /// <exception cref="ArgumentException">Si la cantidad no es válida</exception>
    public int Reponer(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad a reponer debe ser mayor a 0.", nameof(cantidad));

        StockActual += cantidad;
        return StockActual;
    }

    /// <summary>
    /// Desactiva el producto.
    /// </summary>
    public void Desactivar()
    {
        Activo = false;
    }

    /// <summary>
    /// Reactiva el producto.
    /// </summary>
    public void Activar()
    {
        Activo = true;
    }

    /// <summary>
    /// Actualiza el precio del producto.
    /// </summary>
    public void ActualizarPrecio(decimal nuevoPrecio)
    {
        if (nuevoPrecio <= 0)
            throw new ArgumentException("El precio debe ser mayor a 0.", nameof(nuevoPrecio));

        Precio = nuevoPrecio;
    }

    /// <summary>
    /// Actualiza el stock mínimo para alertas.
    /// </summary>
    public void ActualizarStockMinimo(int nuevoStockMinimo)
    {
        if (nuevoStockMinimo < 0)
            throw new ArgumentException("El stock mínimo no puede ser negativo.", nameof(nuevoStockMinimo));

        StockMinimo = nuevoStockMinimo;
    }
}
