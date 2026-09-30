using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad AbonoCuentaPorPagar.
/// </summary>
public class AbonoCuentaPorPagarConfiguration : IEntityTypeConfiguration<AbonoCuentaPorPagar>
{
    public void Configure(EntityTypeBuilder<AbonoCuentaPorPagar> builder)
    {
        builder.ToTable("AbonosCuentaPorPagar");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd();

        builder.Property(a => a.CuentaPorPagarId)
            .IsRequired();

        builder.Property(a => a.Monto)
            .HasColumnType("decimal(12, 2)")
            .IsRequired();

        builder.Property(a => a.MetodoPago)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.Fecha)
            .IsRequired();

        builder.Property(a => a.EmpleadoId)
            .IsRequired();

        // Relación con CuentaPorPagar (Restrict, no Cascade — preservar historial de pagos)
        builder.HasOne<CuentaPorPagar>()
            .WithMany()
            .HasForeignKey(a => a.CuentaPorPagarId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Relación con Empleado que registró el abono
        builder.HasOne<Empleado>()
            .WithMany()
            .HasForeignKey(a => a.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(a => a.CuentaPorPagarId);
    }
}
