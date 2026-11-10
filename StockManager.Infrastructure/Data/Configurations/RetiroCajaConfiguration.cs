using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>Configuración de EF Core para RetiroCaja. Relaciones Restrict: es historial contable.</summary>
public class RetiroCajaConfiguration : IEntityTypeConfiguration<RetiroCaja>
{
    public void Configure(EntityTypeBuilder<RetiroCaja> builder)
    {
        builder.ToTable("RetirosCaja");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        builder.Property(r => r.TurnoCajaId).IsRequired();
        builder.Property(r => r.EmpleadoId).IsRequired();
        builder.Property(r => r.Fecha).IsRequired();
        builder.Property(r => r.Monto).HasColumnType("decimal(12, 2)").IsRequired();
        builder.Property(r => r.Anulado).IsRequired();

        builder.HasOne<TurnoCaja>()
            .WithMany()
            .HasForeignKey(r => r.TurnoCajaId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<Empleado>()
            .WithMany()
            .HasForeignKey(r => r.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<Empleado>()
            .WithMany()
            .HasForeignKey(r => r.AnuladoPorEmpleadoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(r => r.TurnoCajaId);
        builder.HasIndex(r => r.Fecha);
    }
}
