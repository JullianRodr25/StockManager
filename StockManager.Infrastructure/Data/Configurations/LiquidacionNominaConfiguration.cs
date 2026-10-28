using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

public class LiquidacionNominaConfiguration : IEntityTypeConfiguration<LiquidacionNomina>
{
    public void Configure(EntityTypeBuilder<LiquidacionNomina> builder)
    {
        builder.ToTable("LiquidacionesNomina");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedOnAdd();

        builder.Property(l => l.NombreTrabajador).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Cargo).IsRequired().HasMaxLength(100);
        builder.Property(l => l.MetodoPago).IsRequired().HasMaxLength(20);

        builder.Property(l => l.SalarioQuincena).HasColumnType("decimal(12, 2)");
        builder.Property(l => l.AuxilioTransporte).HasColumnType("decimal(12, 2)");
        builder.Property(l => l.OtrosDevengos).HasColumnType("decimal(12, 2)");
        builder.Property(l => l.Devengado).HasColumnType("decimal(12, 2)");
        builder.Property(l => l.DescuentoSalud).HasColumnType("decimal(12, 2)");
        builder.Property(l => l.DescuentoPension).HasColumnType("decimal(12, 2)");
        builder.Property(l => l.OtrosDescuentos).HasColumnType("decimal(12, 2)");
        builder.Property(l => l.DescuentoPrestamos).HasColumnType("decimal(12, 2)");
        builder.Property(l => l.Neto).HasColumnType("decimal(12, 2)");

        // Propiedades calculadas: no son columnas.
        builder.Ignore(l => l.SeguridadSocial);
        builder.Ignore(l => l.TotalDescuentos);

        builder.HasOne<PeriodoNomina>()
            .WithMany()
            .HasForeignKey(l => l.PeriodoNominaId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<Trabajador>()
            .WithMany()
            .HasForeignKey(l => l.TrabajadorId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<TurnoCaja>()
            .WithMany()
            .HasForeignKey(l => l.TurnoCajaId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(l => l.TurnoCajaId);

        // Un trabajador aparece una sola vez por quincena.
        builder.HasIndex(l => new { l.PeriodoNominaId, l.TrabajadorId }).IsUnique();
    }
}
