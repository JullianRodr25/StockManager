using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad Cliente.
/// </summary>
public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.NumeroIdentificacion)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.PasswordHash)
            .IsRequired();

        builder.Property(c => c.Telefono)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Direccion)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(c => c.Activo)
            .IsRequired()
            .HasDefaultValue(true);

        // Índice único en NumeroIdentificacion para login
        builder.HasIndex(c => c.NumeroIdentificacion)
            .IsUnique();

        // Índice en Email para búsquedas de login
        builder.HasIndex(c => c.Email)
            .IsUnique();

        // Índice en Telefono para búsquedas de WhatsApp
        builder.HasIndex(c => c.Telefono);
    }
}
