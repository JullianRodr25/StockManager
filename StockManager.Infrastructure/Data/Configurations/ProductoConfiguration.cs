using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad Producto.
/// Define mapeo de propiedades, relaciones, constraints, índices y RowVersion para concurrencia optimista.
/// </summary>
public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("Productos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();

        builder.Property(p => p.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.CategoriaId)
            .IsRequired();

        builder.Property(p => p.Precio)
            .HasColumnType("decimal(12, 2)")
            .IsRequired();

        builder.Property(p => p.StockActual)
            .IsRequired();

        builder.Property(p => p.StockMinimo)
            .IsRequired();

        builder.Property(p => p.Activo)
            .IsRequired()
            .HasDefaultValue(true);

        // RowVersion para concurrencia optimista — CRÍTICO
        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        // Relación con Categoría
        builder.HasOne<Categoria>()
            .WithMany()
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Índice en Nombre para búsquedas rápidas
        builder.HasIndex(p => p.Nombre)
            .IsUnique(false);

        // Índice compuesto en CategoriaId y Activo para filtros comunes
        builder.HasIndex(p => new { p.CategoriaId, p.Activo });

        // Índice en StockActual para alertas de bajo stock
        builder.HasIndex(p => p.StockActual);

        // Constraints de validación
        builder.HasCheckConstraint("CK_Producto_StockActual_GreaterOrEqual_Zero", "[StockActual] >= 0");
        builder.HasCheckConstraint("CK_Producto_Precio_GreaterOrEqual_Zero", "[Precio] >= 0");
    }
}
