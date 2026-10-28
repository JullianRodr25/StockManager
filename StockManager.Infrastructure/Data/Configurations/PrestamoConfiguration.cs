using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

public class PrestamoConfiguration : IEntityTypeConfiguration<Prestamo>
{
    public void Configure(EntityTypeBuilder<Prestamo> builder)
    {
        builder.ToTable("Prestamos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.Tipo).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Monto).HasColumnType("decimal(12, 2)").IsRequired();
        builder.Property(p => p.NumeroCuotas).IsRequired();
        builder.Property(p => p.ValorCuota).HasColumnType("decimal(12, 2)").IsRequired();
        builder.Property(p => p.PrimeraCuotaDesde).HasColumnType("date").IsRequired();
        builder.Property(p => p.CuotasDescontadas).IsRequired();
        builder.Property(p => p.SaldoPendiente).HasColumnType("decimal(12, 2)").IsRequired();
        builder.Property(p => p.Estado).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Nota).HasMaxLength(300);
        builder.Property(p => p.FechaCreacion).IsRequired();

        builder.HasOne<Trabajador>()
            .WithMany()
            .HasForeignKey(p => p.TrabajadorId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(p => new { p.TrabajadorId, p.Estado });
    }
}
