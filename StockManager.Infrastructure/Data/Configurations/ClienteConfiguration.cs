using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad Cliente.
/// </summary>
public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.NumeroIdentificacion)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Email)
            .HasMaxLength(200);

        builder.Property(c => c.PasswordHash)
            .IsRequired();

        builder.Property(c => c.Telefono)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Direccion)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(c => c.Activo)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.OrigenRegistro)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("Caja");

        builder.Property(c => c.TipoDocumentoFiscal)
            .HasMaxLength(20);

        builder.Property(c => c.NumeroDocumentoFiscal)
            .HasMaxLength(30);

        builder.Property(c => c.RazonSocialFiscal)
            .HasMaxLength(200);

        builder.Property(c => c.DireccionFiscal)
            .HasMaxLength(300);

        builder.Property(c => c.EmailFacturacion)
            .HasMaxLength(200);

        builder.Property(c => c.FotoUrl)
            .HasMaxLength(2048);

        // Propiedad calculada en memoria (igual que Venta.Cambio), no una columna.
        builder.Ignore(c => c.TieneDatosFacturacionElectronicaCompletos);

        // Índice único en NumeroIdentificacion para login
        builder.HasIndex(c => c.NumeroIdentificacion)
            .IsUnique();

        // Índice único en Email para login y recuperación. Filtrado: el correo es opcional y
        // SQL Server solo admite un NULL en un índice único normal, lo que impediría tener dos
        // clientes sin correo.
        builder.HasIndex(c => c.Email)
            .IsUnique()
            .HasFilter("[Email] IS NOT NULL");

        // Índice en Telefono para búsquedas de WhatsApp
        builder.HasIndex(c => c.Telefono);
    }
}
