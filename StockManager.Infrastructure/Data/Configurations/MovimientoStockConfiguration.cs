using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad MovimientoStock.
/// Auditoría de todos los cambios de stock.
/// Índice compuesto (ProductoId, Fecha DESC) para consultas de historial.
/// </summary>
public class MovimientoStockConfiguration : IEntityTypeConfiguration<MovimientoStock>
{
    public void Configure(EntityTypeBuilder<MovimientoStock> builder)
    {
        builder.ToTable("MovimientosStock");

        builder.HasKey(ms => ms.Id);

        builder.Property(ms => ms.Id)
            .ValueGeneratedOnAdd();

        builder.Property(ms => ms.ProductoId)
            .IsRequired();

        builder.Property(ms => ms.Tipo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ms => ms.Cantidad)
            .IsRequired();

        builder.Property(ms => ms.Fecha)
            .IsRequired();

        builder.Property(ms => ms.ReferenciaTipo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ms => ms.ReferenciaId);

        // Relación con Producto — Restrict (NUNCA borrar movimientos al borrar producto)
        builder.HasOne<Producto>()
            .WithMany()
            .HasForeignKey(ms => ms.ProductoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Índice compuesto (ProductoId, Fecha DESC) para historial eficiente
        builder.HasIndex(ms => new { ms.ProductoId, ms.Fecha });

        // Índices adicionales para búsquedas
        builder.HasIndex(ms => ms.Tipo);
        builder.HasIndex(ms => ms.Fecha);

        // Constraint de validación: Cantidad no puede ser 0
        builder.HasCheckConstraint("CK_MovimientoStock_Cantidad_NotZero", "[Cantidad] <> 0");
    }
}
