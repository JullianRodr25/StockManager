using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para CambioMetodoPagoVenta. Relaciones Restrict: es auditoría y no
/// debe desaparecer si se intenta borrar la venta o el empleado.
/// </summary>
public class CambioMetodoPagoVentaConfiguration : IEntityTypeConfiguration<CambioMetodoPagoVenta>
{
    public void Configure(EntityTypeBuilder<CambioMetodoPagoVenta> builder)
    {
        builder.ToTable("CambiosMetodoPagoVenta");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.VentaId).IsRequired();
        builder.Property(c => c.EmpleadoId).IsRequired();
        builder.Property(c => c.Fecha).IsRequired();
        builder.Property(c => c.MetodoAnterior).IsRequired().HasMaxLength(20);
        builder.Property(c => c.MetodoNuevo).IsRequired().HasMaxLength(20);

        builder.HasOne<Venta>()
            .WithMany()
            .HasForeignKey(c => c.VentaId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<Empleado>()
            .WithMany()
            .HasForeignKey(c => c.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(c => c.VentaId);
    }
}
