using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad Venta.
/// </summary>
public class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> builder)
    {
        builder.ToTable("Ventas");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .ValueGeneratedOnAdd();

        builder.Property(v => v.EmpleadoId)
            .IsRequired();

        builder.Property(v => v.ClienteId);

        builder.Property(v => v.Fecha)
            .IsRequired();

        builder.Property(v => v.Total)
            .HasColumnType("decimal(12, 2)")
            .IsRequired();

        builder.Property(v => v.EsCotizacion)
            .IsRequired()
            .HasDefaultValue(false);

        // Relación con Empleado
        builder.HasOne<Empleado>()
            .WithMany()
            .HasForeignKey(v => v.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Relación con Cliente (nullable)
        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(v => v.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices para búsquedas comunes
        builder.HasIndex(v => v.Fecha);
        builder.HasIndex(v => v.EmpleadoId);
        builder.HasIndex(v => v.ClienteId);
    }
}
