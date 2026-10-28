using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

public class PeriodoNominaConfiguration : IEntityTypeConfiguration<PeriodoNomina>
{
    public void Configure(EntityTypeBuilder<PeriodoNomina> builder)
    {
        builder.ToTable("PeriodosNomina");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.FechaInicio).HasColumnType("date").IsRequired();
        builder.Property(p => p.FechaFin).HasColumnType("date").IsRequired();
        builder.Property(p => p.Estado).IsRequired().HasMaxLength(20);
        builder.Property(p => p.FechaCreacion).IsRequired();

        builder.HasOne<Empleado>()
            .WithMany()
            .HasForeignKey(p => p.AprobadaPorEmpleadoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(p => p.FechaInicio).IsUnique();
    }
}
