using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad ResenaProducto.
/// </summary>
public class ResenaProductoConfiguration : IEntityTypeConfiguration<ResenaProducto>
{
    public void Configure(EntityTypeBuilder<ResenaProducto> builder)
    {
        builder.ToTable("ResenasProducto");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd();

        builder.Property(r => r.ProductoId)
            .IsRequired();

        builder.Property(r => r.ClienteId)
            .IsRequired();

        builder.Property(r => r.Calificacion)
            .IsRequired();

        builder.Property(r => r.Comentario)
            .HasMaxLength(ResenaProducto.ComentarioMaxLength)
            .IsRequired(false);

        builder.Property(r => r.FechaCreacion)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(r => r.FechaEdicion)
            .HasColumnType("datetime2")
            .IsRequired(false);

        // Relación con Producto — Cascade: si se borra físicamente un producto (no pasa en la
        // práctica, solo se desactiva), sus reseñas se van con él. Mismo criterio que
        // ProductoFoto → Producto.
        builder.HasOne<Producto>()
            .WithMany()
            .HasForeignKey(r => r.ProductoId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // Relación con Cliente — Restrict: si un cliente se desactiva (nunca se borra
        // físicamente), sus reseñas publicadas se mantienen visibles en el catálogo.
        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(r => r.ClienteId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Regla de negocio: un cliente solo puede tener UNA reseña por producto.
        builder.HasIndex(r => new { r.ProductoId, r.ClienteId })
            .IsUnique();

        builder.HasCheckConstraint("CK_ResenaProducto_Calificacion_Between_One_And_Five",
            "[Calificacion] >= 1 AND [Calificacion] <= 5");
    }
}
