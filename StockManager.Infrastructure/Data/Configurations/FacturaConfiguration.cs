using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad Factura.
/// Constraint: exactamente una de VentaId o PedidoId debe estar presente.
/// </summary>
public class FacturaConfiguration : IEntityTypeConfiguration<Factura>
{
    public void Configure(EntityTypeBuilder<Factura> builder)
    {
        builder.ToTable("Facturas");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .ValueGeneratedOnAdd();

        builder.Property(f => f.VentaId);

        builder.Property(f => f.PedidoId);

        builder.Property(f => f.Numero)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(f => f.Fecha)
            .IsRequired();

        builder.Property(f => f.Total)
            .HasColumnType("decimal(12, 2)")
            .IsRequired();

        // Relación con Venta (nullable)
        builder.HasOne<Venta>()
            .WithMany()
            .HasForeignKey(f => f.VentaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relación con Pedido (nullable)
        builder.HasOne<Pedido>()
            .WithMany()
            .HasForeignKey(f => f.PedidoId)
            .OnDelete(DeleteBehavior.Restrict);

        // CHECK constraint: exactamente una de VentaId o PedidoId debe estar presente
        // SQL Server syntax: (VentaId IS NOT NULL AND PedidoId IS NULL) OR (VentaId IS NULL AND PedidoId IS NOT NULL)
        builder.HasCheckConstraint(
            "CK_Factura_ExactlyOneReference",
            "(" +
            "CASE WHEN VentaId IS NOT NULL AND PedidoId IS NULL THEN 1 " +
            "WHEN VentaId IS NULL AND PedidoId IS NOT NULL THEN 1 " +
            "ELSE 0 END = 1" +
            ")");

        // Índices
        builder.HasIndex(f => f.VentaId);
        builder.HasIndex(f => f.PedidoId);
        // Índice ÚNICO en Numero permitiendo múltiples NULL (SQL Server: filtro excluye NULLs)
        builder.HasIndex(f => f.Numero)
            .IsUnique()
            .HasFilter("[Numero] IS NOT NULL");
        builder.HasIndex(f => f.Fecha);
    }
}
