using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad DetallePedido.
/// Estado de cumplimiento por línea, no por pedido completo.
/// </summary>
public class DetallePedidoConfiguration : IEntityTypeConfiguration<DetallePedido>
{
    public void Configure(EntityTypeBuilder<DetallePedido> builder)
    {
        builder.ToTable("DetallesPedido");

        builder.HasKey(dp => dp.Id);

        builder.Property(dp => dp.Id)
            .ValueGeneratedOnAdd();

        builder.Property(dp => dp.PedidoId)
            .IsRequired();

        builder.Property(dp => dp.ProductoId)
            .IsRequired();

        builder.Property(dp => dp.Cantidad)
            .IsRequired();

        builder.Property(dp => dp.EstadoLinea)
            .IsRequired()
            .HasMaxLength(50);

        // Relación con Pedido — Cascade
        builder.HasOne<Pedido>()
            .WithMany()
            .HasForeignKey(dp => dp.PedidoId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // Relación con Producto — Restrict
        builder.HasOne<Producto>()
            .WithMany()
            .HasForeignKey(dp => dp.ProductoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Índices
        builder.HasIndex(dp => dp.PedidoId);
        builder.HasIndex(dp => dp.ProductoId);
        builder.HasIndex(dp => dp.EstadoLinea);
    }
}
