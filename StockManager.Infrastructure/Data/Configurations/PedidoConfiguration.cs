using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad Pedido.
/// </summary>
public class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();

        builder.Property(p => p.ClienteId)
            .IsRequired();

        builder.Property(p => p.Fecha)
            .IsRequired();

        builder.Property(p => p.Estado)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Direccion)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(p => p.Total)
            .IsRequired()
            .HasColumnType("decimal(12, 2)");

        builder.Property(p => p.VentaId);

        // Relación con Cliente
        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(p => p.ClienteId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Relación con Venta (nullable) — la venta generada al marcar el pedido como Entregado
        builder.HasOne<Venta>()
            .WithMany()
            .HasForeignKey(p => p.VentaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices
        builder.HasIndex(p => p.ClienteId);
        builder.HasIndex(p => p.Fecha);
        builder.HasIndex(p => p.Estado);
        builder.HasIndex(p => p.VentaId);
    }
}
