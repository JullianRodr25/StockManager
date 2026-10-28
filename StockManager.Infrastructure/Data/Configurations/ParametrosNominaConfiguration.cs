using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

public class ParametrosNominaConfiguration : IEntityTypeConfiguration<ParametrosNomina>
{
    public void Configure(EntityTypeBuilder<ParametrosNomina> builder)
    {
        builder.ToTable("ParametrosNomina");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.SalarioMinimo).HasColumnType("decimal(12, 2)").IsRequired();
        builder.Property(p => p.AuxilioTransporte).HasColumnType("decimal(12, 2)").IsRequired();
        builder.Property(p => p.LimiteSalariosAuxilio).HasColumnType("decimal(6, 2)").IsRequired();
        builder.Property(p => p.PorcentajeSaludTrabajador).HasColumnType("decimal(5, 2)").IsRequired();
        builder.Property(p => p.PorcentajePensionTrabajador).HasColumnType("decimal(5, 2)").IsRequired();
        builder.Property(p => p.FechaActualizacion).IsRequired();
    }
}
