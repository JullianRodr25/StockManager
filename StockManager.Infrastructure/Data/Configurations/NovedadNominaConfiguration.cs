using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

public class NovedadNominaConfiguration : IEntityTypeConfiguration<NovedadNomina>
{
    public void Configure(EntityTypeBuilder<NovedadNomina> builder)
    {
        builder.ToTable("NovedadesNomina");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedOnAdd();

        builder.Property(n => n.Tipo).IsRequired().HasMaxLength(20);
        builder.Property(n => n.Concepto).IsRequired().HasMaxLength(100);
        builder.Property(n => n.Valor).HasColumnType("decimal(12, 2)").IsRequired();
        builder.Property(n => n.FechaCreacion).IsRequired();

        builder.HasOne<PeriodoNomina>()
            .WithMany()
            .HasForeignKey(n => n.PeriodoNominaId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<Trabajador>()
            .WithMany()
            .HasForeignKey(n => n.TrabajadorId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(n => new { n.PeriodoNominaId, n.TrabajadorId });
    }
}
