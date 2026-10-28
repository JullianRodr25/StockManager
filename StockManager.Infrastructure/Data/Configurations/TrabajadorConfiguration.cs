using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

public class TrabajadorConfiguration : IEntityTypeConfiguration<Trabajador>
{
    public void Configure(EntityTypeBuilder<Trabajador> builder)
    {
        builder.ToTable("Trabajadores");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();

        builder.Property(t => t.Nombre).IsRequired().HasMaxLength(200);
        builder.Property(t => t.NumeroIdentificacion).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Telefono).HasMaxLength(20);
        builder.Property(t => t.Cargo).IsRequired().HasMaxLength(100);
        builder.Property(t => t.TipoContrato).IsRequired().HasMaxLength(30);
        builder.Property(t => t.FechaIngreso).HasColumnType("date").IsRequired();
        builder.Property(t => t.FechaFinContrato).HasColumnType("date");
        builder.Property(t => t.SalarioMensual).HasColumnType("decimal(12, 2)").IsRequired();
        builder.Property(t => t.AplicaSeguridadSocial).IsRequired();
        builder.Property(t => t.MetodoPago).IsRequired().HasMaxLength(20);
        builder.Property(t => t.Activo).IsRequired().HasDefaultValue(true);
        builder.Property(t => t.FechaCreacion).IsRequired();

        builder.HasIndex(t => t.NumeroIdentificacion).IsUnique();
        builder.HasIndex(t => t.Activo);
    }
}
