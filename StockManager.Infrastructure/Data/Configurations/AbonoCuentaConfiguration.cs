using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad AbonoCuenta.
/// </summary>
public class AbonoCuentaConfiguration : IEntityTypeConfiguration<AbonoCuenta>
{
    public void Configure(EntityTypeBuilder<AbonoCuenta> builder)
    {
        builder.ToTable("AbonosCuenta");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd();

        builder.Property(a => a.VentaId)
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

        // Relación con Venta (Restrict, no Cascade — preservar historial de pagos)
        builder.HasOne<Venta>()
            .WithMany()
            .HasForeignKey(a => a.VentaId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Relación con Empleado que registró el abono
        builder.HasOne<Empleado>()
            .WithMany()
            .HasForeignKey(a => a.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(a => a.VentaId);
    }
}
