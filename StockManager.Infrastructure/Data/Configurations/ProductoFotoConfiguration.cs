using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad ProductoFoto.
/// </summary>
public class ProductoFotoConfiguration : IEntityTypeConfiguration<ProductoFoto>
{
    public void Configure(EntityTypeBuilder<ProductoFoto> builder)
    {
        builder.ToTable("ProductoFotos");

        builder.HasKey(pf => pf.Id);

        builder.Property(pf => pf.Id)
            .ValueGeneratedOnAdd();

        builder.Property(pf => pf.ProductoId)
            .IsRequired();

        builder.Property(pf => pf.Url)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(pf => pf.Orden)
            .IsRequired();

        // Relación con Producto — Cascade: si se borra físicamente un producto (no pasa en la
        // práctica, solo se desactiva), sus fotos se van con él. Mismo criterio que
        // DetallePedido → Pedido.
        builder.HasOne<Producto>()
            .WithMany()
            .HasForeignKey(pf => pf.ProductoId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // Índice por ProductoId (lookup principal: "fotos de este producto") + Orden para que
        // el ORDER BY del carrusel no requiera un sort adicional en memoria.
        builder.HasIndex(pf => new { pf.ProductoId, pf.Orden });
    }
}
