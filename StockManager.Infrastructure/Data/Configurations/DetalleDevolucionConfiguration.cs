using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para DetalleDevolucion. Cascade con su Devolucion (las líneas no
/// existen sin ella); Restrict con Producto y DetalleVenta para preservar el historial.
/// </summary>
public class DetalleDevolucionConfiguration : IEntityTypeConfiguration<DetalleDevolucion>
{
    public void Configure(EntityTypeBuilder<DetalleDevolucion> builder)
    {
        builder.ToTable("DetallesDevolucion");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedOnAdd();

        builder.Property(d => d.DevolucionId).IsRequired();
        builder.Property(d => d.Sentido).IsRequired().HasMaxLength(20);
        builder.Property(d => d.ProductoId).IsRequired();
        builder.Property(d => d.Cantidad).IsRequired();
        builder.Property(d => d.PrecioUnitario).HasColumnType("decimal(12, 2)").IsRequired();
        builder.Property(d => d.ReintegradoAlStock).IsRequired();

        builder.HasOne<Devolucion>()
            .WithMany()
            .HasForeignKey(d => d.DevolucionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne<Producto>()
            .WithMany()
            .HasForeignKey(d => d.ProductoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<DetalleVenta>()
            .WithMany()
            .HasForeignKey(d => d.DetalleVentaId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(d => d.DevolucionId);
        builder.HasIndex(d => d.DetalleVentaId);

        builder.HasCheckConstraint("CK_DetalleDevolucion_Cantidad_Positiva", "[Cantidad] > 0");
    }
}
