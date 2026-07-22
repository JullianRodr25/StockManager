using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad Empleado.
/// </summary>
public class EmpleadoConfiguration : IEntityTypeConfiguration<Empleado>
{
    public void Configure(EntityTypeBuilder<Empleado> builder)
    {
        builder.ToTable("Empleados");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        builder.Property(e => e.NumeroIdentificacion)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Nombre)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.PasswordHash)
            .IsRequired();

        builder.Property(e => e.Rol)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Activo)
            .IsRequired()
            .HasDefaultValue(true);

        // Índice único en NumeroIdentificacion para login
        builder.HasIndex(e => e.NumeroIdentificacion)
            .IsUnique();

        // Índice en Email para búsquedas de login
        builder.HasIndex(e => e.Email)
            .IsUnique();
    }
}
