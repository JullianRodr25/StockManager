using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad BackorderRequest.
/// Índice filtrado en ProductoId WHERE Estado = 'Pendiente' para consultas eficientes.
/// </summary>
public class BackorderRequestConfiguration : IEntityTypeConfiguration<BackorderRequest>
{
    public void Configure(EntityTypeBuilder<BackorderRequest> builder)
    {
        builder.ToTable("BackorderRequests");

        builder.HasKey(br => br.Id);

        builder.Property(br => br.Id)
            .ValueGeneratedOnAdd();

        builder.Property(br => br.ClienteId)
            .IsRequired();

        builder.Property(br => br.ProductoId)
            .IsRequired();

        builder.Property(br => br.CantidadDeseada)
            .IsRequired();

        builder.Property(br => br.FechaSolicitud)
            .IsRequired();

        builder.Property(br => br.Estado)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(br => br.FechaNotificacion);

        // Relación con Cliente
        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(br => br.ClienteId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Relación con Producto — Restrict
        builder.HasOne<Producto>()
            .WithMany()
            .HasForeignKey(br => br.ProductoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Índice filtrado: solo solicitudes pendientes (para búsqueda rápida de items a notificar)
        builder.HasIndex(br => new { br.ProductoId, br.Estado })
            .HasFilter("Estado = 'Pendiente'");

        // Índices adicionales
        builder.HasIndex(br => br.ClienteId);
        builder.HasIndex(br => br.Estado);
        builder.HasIndex(br => br.FechaSolicitud);
    }
}
