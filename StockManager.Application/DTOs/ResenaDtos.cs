namespace StockManager.Application.DTOs;

/// <summary>
/// DTO de una reseña de producto, tal como se muestra en el catálogo de la PWA.
/// </summary>
/// <param name="EsPropia">
/// True si esta reseña pertenece al cliente autenticado que hizo la petición — el frontend la
/// usa para decidir si muestra los botones de editar/eliminar sobre esa reseña en particular.
/// Se calcula en el service comparando ClienteId contra el cliente del token, nunca viaja como
/// columna de BD.
/// </param>
public record ResenaResponse(
    int Id,
    int ClienteId,
    string ClienteNombre,
    int Calificacion,
    string? Comentario,
    DateTime FechaCreacion,
    DateTime? FechaEdicion,
    bool EsPropia
);

/// <summary>
/// DTO para crear una reseña nueva. Calificacion es obligatoria (1-5); Comentario es opcional.
/// </summary>
public class CrearResenaRequest
{
    public int Calificacion { get; set; }
    public string? Comentario { get; set; }
}

/// <summary>
/// DTO para editar una reseña existente — misma forma que crear, el cliente reemplaza
/// calificación y comentario de la reseña que ya tenía en ese producto.
/// </summary>
public class EditarResenaRequest
{
    public int Calificacion { get; set; }
    public string? Comentario { get; set; }
}
