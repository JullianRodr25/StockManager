using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad DetallePagoVenta.
/// </summary>
public class DetallePagoVentaConfiguration : IEntityTypeConfiguration<DetallePagoVenta>
{
    public void Configure(EntityTypeBuilder<DetallePagoVenta> builder)
    {
        builder.ToTable("DetallesPagoVenta");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedOnAdd();

        builder.Property(d => d.VentaId)
            .IsRequired();

        builder.Property(d => d.MetodoPago)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(d => d.Monto)
            .HasColumnType("decimal(12, 2)")
            .IsRequired();

        // Relación con Venta (Restrict, no Cascade — preservar historial de pagos, igual que AbonoCuenta).
        builder.HasOne<Venta>()
            .WithMany()
            .HasForeignKey(d => d.VentaId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(d => d.VentaId);
    }
}
