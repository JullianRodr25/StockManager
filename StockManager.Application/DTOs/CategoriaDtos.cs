using System.ComponentModel.DataAnnotations;

namespace StockManager.Application.DTOs;

/// <summary>
/// DTO para la respuesta de una categoría.
/// </summary>
public class CategoriaResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
}

/// <summary>
/// DTO para crear una nueva categoría.
/// </summary>
public class CrearCategoriaRequest
{
    [Required(ErrorMessage = "El nombre de la categoría es requerido")]
    [MaxLength(100, ErrorMessage = "El nombre de la categoría no puede exceder 100 caracteres")]
    public string Nombre { get; set; } = null!;
}
