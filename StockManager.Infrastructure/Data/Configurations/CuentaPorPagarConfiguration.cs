using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad CuentaPorPagar.
/// </summary>
public class CuentaPorPagarConfiguration : IEntityTypeConfiguration<CuentaPorPagar>
{
    public void Configure(EntityTypeBuilder<CuentaPorPagar> builder)
    {
        builder.ToTable("CuentasPorPagar", t =>
        {
            t.HasCheckConstraint("CK_CuentaPorPagar_MontoTotal_GreaterThanZero", "[MontoTotal] > 0");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.ProveedorId)
            .IsRequired();

        builder.Property(c => c.Concepto)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(c => c.MontoTotal)
            .HasColumnType("decimal(12, 2)")
            .IsRequired();

        builder.Property(c => c.FechaCompra)
            .IsRequired();

        builder.Property(c => c.FechaVencimiento)
            .IsRequired();

        builder.Property(c => c.Estado)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.FechaUltimaAlerta);

        // Restrict, no Cascade: la historia de cuentas por pagar de un proveedor se conserva
        // aunque el proveedor se desactive (no se borran proveedores, solo se desactivan).
        builder.HasOne<Proveedor>()
            .WithMany()
            .HasForeignKey(c => c.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(c => c.ProveedorId);
        builder.HasIndex(c => c.Estado);
        builder.HasIndex(c => c.FechaVencimiento);
    }
}
