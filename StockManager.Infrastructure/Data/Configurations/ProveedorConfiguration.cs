using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad Proveedor.
/// </summary>
public class ProveedorConfiguration : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        builder.ToTable("Proveedores");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();

        builder.Property(p => p.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.NumeroIdentificacion)
            .HasMaxLength(50);

        builder.Property(p => p.Telefono)
            .HasMaxLength(20);

        builder.Property(p => p.Email)
            .HasMaxLength(200);

        builder.Property(p => p.Direccion)
            .HasMaxLength(300);

        builder.Property(p => p.Activo)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(p => p.Nombre);

        // Único solo entre los que sí tienen NIT informado (muchos proveedores pequeños no lo tendrán).
        builder.HasIndex(p => p.NumeroIdentificacion)
            .IsUnique()
            .HasFilter("[NumeroIdentificacion] IS NOT NULL");
    }
}
