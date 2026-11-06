using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para Devolucion. Todas las relaciones son Restrict: una devolución es
/// historial contable y no debe desaparecer si se intenta borrar la venta, el empleado o el turno.
/// </summary>
public class DevolucionConfiguration : IEntityTypeConfiguration<Devolucion>
{
    public void Configure(EntityTypeBuilder<Devolucion> builder)
    {
        builder.ToTable("Devoluciones");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedOnAdd();

        builder.Property(d => d.VentaId).IsRequired();
        builder.Property(d => d.EmpleadoId).IsRequired();
        builder.Property(d => d.Fecha).IsRequired();

        builder.Property(d => d.Motivo)
            .IsRequired()
            .HasMaxLength(Devolucion.LongitudMaximaMotivo);

        builder.Property(d => d.TotalDevuelto).HasColumnType("decimal(12, 2)").IsRequired();
        builder.Property(d => d.TotalCambio).HasColumnType("decimal(12, 2)").IsRequired();

        builder.Property(d => d.MetodoPago).HasMaxLength(20);
        builder.Property(d => d.RequiereNotaCredito).IsRequired();

        builder.HasOne<Venta>()
            .WithMany()
            .HasForeignKey(d => d.VentaId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<Empleado>()
            .WithMany()
            .HasForeignKey(d => d.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<TurnoCaja>()
            .WithMany()
            .HasForeignKey(d => d.TurnoCajaId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(d => d.VentaId);
        builder.HasIndex(d => d.Fecha);
        builder.HasIndex(d => d.TurnoCajaId);
    }
}
