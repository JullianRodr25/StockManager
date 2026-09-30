using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad NotificacionInterna.
/// </summary>
public class NotificacionInternaConfiguration : IEntityTypeConfiguration<NotificacionInterna>
{
    public void Configure(EntityTypeBuilder<NotificacionInterna> builder)
    {
        builder.ToTable("NotificacionesInternas");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .ValueGeneratedOnAdd();

        builder.Property(n => n.Tipo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(n => n.Titulo)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Mensaje)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(n => n.EntidadTipo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(n => n.EntidadId)
            .IsRequired();

        builder.Property(n => n.FechaCreacion)
            .IsRequired();

        builder.Property(n => n.Leida)
            .IsRequired()
            .HasDefaultValue(false);

        // Índice para la consulta más común: no leídas, más recientes primero.
        builder.HasIndex(n => new { n.Leida, n.FechaCreacion });
    }
}
