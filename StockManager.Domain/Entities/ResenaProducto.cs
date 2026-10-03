namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad ResenaProducto del dominio.
/// Representa la reseña (calificación 1-5 + comentario opcional) que un cliente deja sobre un
/// producto. Entidad plana con ProductoId/ClienteId como FK, sin colecciones de navegación en
/// Producto ni en Cliente — mismo patrón que ProductoFoto: el padre no conoce a sus hijos vía
/// EF, quien los administra es el service (ResenaService), consultando AppDbContext
/// directamente.
///
/// Regla de negocio: un cliente solo puede tener UNA reseña por producto (reforzado con un
/// índice único en (ProductoId, ClienteId) en ResenaProductoConfiguration); si quiere cambiar
/// de opinión, edita o borra la que ya tiene en vez de crear una nueva.
/// </summary>
public class ResenaProducto
{
    /// <summary>
    /// Longitud máxima del comentario. Es un comentario de catálogo (tipo Homecenter/Amazon),
    /// no un campo de texto libre extenso.
    /// </summary>
    public const int ComentarioMaxLength = 1000;

    public int Id { get; private set; }
    public int ProductoId { get; private set; }
    public int ClienteId { get; private set; }
    public int Calificacion { get; private set; }
    public string? Comentario { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaEdicion { get; private set; }

    private ResenaProducto() { }

    public static ResenaProducto Crear(int productoId, int clienteId, int calificacion, string? comentario)
    {
        if (productoId <= 0)
            throw new ArgumentException("ProductoId debe ser mayor a 0.", nameof(productoId));

        if (clienteId <= 0)
            throw new ArgumentException("ClienteId debe ser mayor a 0.", nameof(clienteId));

        ValidarCalificacion(calificacion);
        ValidarComentario(comentario);

        return new ResenaProducto
        {
            ProductoId = productoId,
            ClienteId = clienteId,
            Calificacion = calificacion,
            Comentario = NormalizarComentario(comentario),
            FechaCreacion = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Edita la calificación/comentario de una reseña ya existente (el cliente cambió de
    /// opinión). No se puede editar ProductoId/ClienteId: para eso habría que borrar y crear
    /// otra en un producto distinto.
    /// </summary>
    public void Editar(int calificacion, string? comentario)
    {
        ValidarCalificacion(calificacion);
        ValidarComentario(comentario);

        Calificacion = calificacion;
        Comentario = NormalizarComentario(comentario);
        FechaEdicion = DateTime.UtcNow;
    }

    private static void ValidarCalificacion(int calificacion)
    {
        if (calificacion is < 1 or > 5)
            throw new ArgumentException("La calificación debe estar entre 1 y 5.", nameof(calificacion));
    }

    private static void ValidarComentario(string? comentario)
    {
        if (comentario != null && comentario.Length > ComentarioMaxLength)
            throw new ArgumentException(
                $"El comentario no puede exceder {ComentarioMaxLength} caracteres.", nameof(comentario));
    }

    private static string? NormalizarComentario(string? comentario) =>
        string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();
}
