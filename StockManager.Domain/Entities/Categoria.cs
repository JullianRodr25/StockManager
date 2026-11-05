namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad Categoria del dominio.
/// Representa una categoría de productos en el inventario.
/// </summary>
public class Categoria
{
    public int Id { get; private set; }
    public string Nombre { get; private set; } = null!;

    // Constructor privado para EF Core
    private Categoria() { }

    /// <summary>
    /// Factory method para crear una nueva Categoría con validaciones.
    /// </summary>
    public static Categoria Crear(string nombre)
    {
        return new Categoria { Nombre = NormalizarNombre(nombre) };
    }

    /// <summary>Cambia el nombre aplicando las mismas reglas que al crearla.</summary>
    public void Renombrar(string nombre)
    {
        Nombre = NormalizarNombre(nombre);
    }

    private static string NormalizarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la categoría no puede estar vacío.", nameof(nombre));

        if (nombre.Length > 100)
            throw new ArgumentException("El nombre de la categoría no puede exceder 100 caracteres.", nameof(nombre));

        return nombre.Trim();
    }
}
