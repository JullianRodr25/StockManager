using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad NotificacionLog.
/// Trazabilidad de todos los envíos de notificaciones.
/// </summary>
public class NotificacionLogConfiguration : IEntityTypeConfiguration<NotificacionLog>
{
    public void Configure(EntityTypeBuilder<NotificacionLog> builder)
    {
        builder.ToTable("NotificacionesLog");

        builder.HasKey(nl => nl.Id);

        builder.Property(nl => nl.Id)
            .ValueGeneratedOnAdd();

        builder.Property(nl => nl.Canal)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(nl => nl.Destinatario)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(nl => nl.ReferenciaTipo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(nl => nl.ReferenciaId)
            .IsRequired();

        builder.Property(nl => nl.Estado)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(nl => nl.FechaEnvio)
            .IsRequired();

        builder.Property(nl => nl.DetalleError)
            .HasMaxLength(1000);

        // Índices para búsquedas y auditoría
        builder.HasIndex(nl => nl.Canal);
        builder.HasIndex(nl => nl.Estado);
        builder.HasIndex(nl => nl.FechaEnvio);
        builder.HasIndex(nl => new { nl.ReferenciaTipo, nl.ReferenciaId });
        builder.HasIndex(nl => nl.Destinatario);
    }
}
