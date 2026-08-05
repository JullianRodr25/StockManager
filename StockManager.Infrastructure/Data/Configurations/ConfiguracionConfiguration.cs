using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

public class ConfiguracionConfiguration : IEntityTypeConfiguration<Configuracion>
{
    public void Configure(EntityTypeBuilder<Configuracion> builder)
    {
        builder.ToTable("Configuracion");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.TarifaIvaPorDefecto)
            .HasColumnType("decimal(5, 2)")
            .IsRequired();

        builder.HasData(new
        {
            Id = 1,
            TarifaIvaPorDefecto = 19.00m
        });
    }
}