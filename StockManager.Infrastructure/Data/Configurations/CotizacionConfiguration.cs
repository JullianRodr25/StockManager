using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para Cotizacion y sus líneas. Las líneas pertenecen al agregado
/// (cascada). Las referencias a Empleado, Cliente y Venta son Restrict: una cotización es
/// historial comercial y no debe desaparecer por borrar a quien la hizo.
/// </summary>
public class CotizacionConfiguration : IEntityTypeConfiguration<Cotizacion>
{
    public void Configure(EntityTypeBuilder<Cotizacion> builder)
    {
        builder.ToTable("Cotizaciones");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.EmpleadoId).IsRequired();
        builder.Property(c => c.NombreCliente).HasMaxLength(200);
        builder.Property(c => c.TelefonoCliente).HasMaxLength(50);
        builder.Property(c => c.EmailCliente).HasMaxLength(200);
        builder.Property(c => c.Fecha).IsRequired();
        builder.Property(c => c.FechaVencimiento).IsRequired();
        builder.Property(c => c.Notas).HasMaxLength(Cotizacion.MaximoCaracteresNotas);
        builder.Property(c => c.Total).HasColumnType("decimal(12, 2)").IsRequired();
        builder.Property(c => c.Estado).IsRequired().HasMaxLength(20);

        builder.HasOne<Empleado>()
            .WithMany()
            .HasForeignKey(c => c.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(c => c.ClienteId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne<Venta>()
            .WithMany()
            .HasForeignKey(c => c.VentaId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasMany(c => c.Detalles)
            .WithOne()
            .HasForeignKey(d => d.CotizacionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Detalles).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(c => c.Estado);
        builder.HasIndex(c => c.Fecha);
        builder.HasIndex(c => c.VentaId);
    }
}

public class DetalleCotizacionConfiguration : IEntityTypeConfiguration<DetalleCotizacion>
{
    public void Configure(EntityTypeBuilder<DetalleCotizacion> builder)
    {
        builder.ToTable("DetallesCotizacion");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedOnAdd();

        builder.Property(d => d.CotizacionId).IsRequired();
        builder.Property(d => d.ProductoId).IsRequired();
        builder.Property(d => d.Cantidad).IsRequired();
        builder.Property(d => d.PrecioUnitario).HasColumnType("decimal(12, 2)").IsRequired();
        builder.Ignore(d => d.Subtotal);

        builder.HasOne<Producto>()
            .WithMany()
            .HasForeignKey(d => d.ProductoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
