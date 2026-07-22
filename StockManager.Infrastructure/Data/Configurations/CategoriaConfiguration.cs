using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad Categoria.
/// Define constraint de unicidad en Nombre.
/// </summary>
public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("Categorias");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        // Constraint de unicidad en Nombre
        builder.HasIndex(c => c.Nombre)
            .IsUnique();
    }
}
