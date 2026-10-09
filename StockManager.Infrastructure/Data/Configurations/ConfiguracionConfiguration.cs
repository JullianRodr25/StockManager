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

        builder.Property(c => c.CiudadEmpresa).HasMaxLength(100);
        builder.Property(c => c.BarrioEmpresa).HasMaxLength(100);
        builder.Property(c => c.ResponsabilidadIvaEmpresa).HasMaxLength(100);
        builder.Property(c => c.ActividadEconomicaEmpresa).HasMaxLength(50);
        builder.Property(c => c.ResolucionDianNumero).HasMaxLength(50);
        builder.Property(c => c.ResolucionDianFecha).HasColumnType("date");
        builder.Property(c => c.ResolucionDianPrefijo).HasMaxLength(10);
        builder.Property(c => c.TextoLegalFactura).HasMaxLength(600);
        builder.Property(c => c.PoliticaCambiosFactura).HasMaxLength(300);

        builder.HasData(new
        {
            Id = 1,
            TarifaIvaPorDefecto = 19.00m
        });
    }
}