namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad ProductoFoto del dominio.
/// Representa una foto dentro de la galería de un producto (catálogo tipo Homecenter: varias
/// fotos por producto, mostradas en carrusel). Entidad plana con ProductoId como FK, sin
/// colección de navegación en Producto — mismo patrón que DetallePedido/DetalleVenta en este
/// codebase: el padre no conoce a sus hijos vía EF, quien los administra es el service
/// (ProductoService), consultando StockManager.Infrastructure.Data.AppDbContext directamente.
/// </summary>
public class ProductoFoto
{
    public int Id { get; private set; }
    public int ProductoId { get; private set; }

    /// <summary>
    /// URL pública del blob (Azure Blob Storage), igual que Cliente/foto de perfil.
    /// </summary>
    public string Url { get; private set; } = null!;

    /// <summary>
    /// Orden de aparición en el carrusel (0 = primera/portada). Se asigna como
    /// "máximo actual + 1" al agregar una foto; no se reindexa al eliminar una (los huecos no
    /// afectan el orden relativo, solo se usa para ORDER BY).
    /// </summary>
    public int Orden { get; private set; }

    private ProductoFoto() { }

    public static ProductoFoto Crear(int productoId, string url, int orden)
    {
        if (productoId <= 0)
            throw new ArgumentException("ProductoId debe ser mayor a 0.", nameof(productoId));

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("La URL de la foto es requerida.", nameof(url));

        if (orden < 0)
            throw new ArgumentException("El orden no puede ser negativo.", nameof(orden));

        return new ProductoFoto
        {
            ProductoId = productoId,
            Url = url,
            Orden = orden
        };
    }
}
