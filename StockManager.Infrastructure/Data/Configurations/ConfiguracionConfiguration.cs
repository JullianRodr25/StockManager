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

        builder.Property(c => c.TelefonoNotificacionesAdmin)
            .HasMaxLength(20);

        builder.Property(c => c.NombreImpresoraTickets)
            .HasMaxLength(200);

        builder.Property(c => c.NombreEmpresa)
            .HasMaxLength(200);

        builder.Property(c => c.NitEmpresa)
            .HasMaxLength(30);

        builder.Property(c => c.DireccionEmpresa)
            .HasMaxLength(300);

        builder.Property(c => c.TelefonoEmpresa)
            .HasMaxLength(30);

        builder.Property(c => c.EmailEmpresa)
            .HasMaxLength(200);

        builder.HasData(new
        {
            Id = 1,
            TarifaIvaPorDefecto = 19.00m
        });
    }
}