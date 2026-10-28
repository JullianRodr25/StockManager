using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

public class CuotaPrestamoConfiguration : IEntityTypeConfiguration<CuotaPrestamo>
{
    public void Configure(EntityTypeBuilder<CuotaPrestamo> builder)
    {
        builder.ToTable("CuotasPrestamo");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.Numero).IsRequired();
        builder.Property(c => c.Valor).HasColumnType("decimal(12, 2)").IsRequired();
        builder.Property(c => c.Aplicada).IsRequired();

        builder.HasOne<PeriodoNomina>()
            .WithMany()
            .HasForeignKey(c => c.PeriodoNominaId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<Prestamo>()
            .WithMany()
            .HasForeignKey(c => c.PrestamoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Un préstamo descuenta una sola cuota por quincena.
        builder.HasIndex(c => new { c.PeriodoNominaId, c.PrestamoId }).IsUnique();
    }
}
