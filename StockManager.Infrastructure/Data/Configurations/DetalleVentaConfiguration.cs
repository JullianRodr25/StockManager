using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad DetalleVenta.
/// Relación Cascade con Venta — si se borra la venta, se borran sus detalles.
/// Relación Restrict con Producto — nunca borramos productos en cascada para preservar historial.
/// </summary>
public class DetalleVentaConfiguration : IEntityTypeConfiguration<DetalleVenta>
{
    public void Configure(EntityTypeBuilder<DetalleVenta> builder)
    {
        builder.ToTable("DetallesVenta");

        builder.HasKey(dv => dv.Id);

        builder.Property(dv => dv.Id)
            .ValueGeneratedOnAdd();

        builder.Property(dv => dv.VentaId)
            .IsRequired();

        builder.Property(dv => dv.ProductoId)
            .IsRequired();

        builder.Property(dv => dv.Cantidad)
            .IsRequired();

        builder.Property(dv => dv.PrecioUnitario)
            .HasColumnType("decimal(12, 2)")
            .IsRequired();

        // Relación con Venta — Cascade
        builder.HasOne<Venta>()
            .WithMany()
            .HasForeignKey(dv => dv.VentaId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // Relación con Producto — Restrict (preservar historial)
        builder.HasOne<Producto>()
            .WithMany()
            .HasForeignKey(dv => dv.ProductoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Índices
        builder.HasIndex(dv => dv.VentaId);
        builder.HasIndex(dv => dv.ProductoId);
    }
}
