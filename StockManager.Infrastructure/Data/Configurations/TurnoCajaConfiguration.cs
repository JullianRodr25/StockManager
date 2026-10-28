using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad TurnoCaja.
/// </summary>
public class TurnoCajaConfiguration : IEntityTypeConfiguration<TurnoCaja>
{
    public void Configure(EntityTypeBuilder<TurnoCaja> builder)
    {
        builder.ToTable("TurnosCaja");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedOnAdd();

        builder.Property(t => t.EmpleadoId).IsRequired();

        builder.Property(t => t.FechaApertura).IsRequired();

        builder.Property(t => t.BaseInicial)
            .HasColumnType("decimal(12, 2)")
            .IsRequired();

        builder.Property(t => t.Estado)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(t => t.EfectivoContado).HasColumnType("decimal(12, 2)");
        builder.Property(t => t.EfectivoEsperado).HasColumnType("decimal(12, 2)");
        builder.Property(t => t.Diferencia).HasColumnType("decimal(12, 2)");
        builder.Property(t => t.TotalVentasEfectivo).HasColumnType("decimal(12, 2)");
        builder.Property(t => t.TotalAbonosFiadoEfectivo).HasColumnType("decimal(12, 2)");
        builder.Property(t => t.TotalPagosProveedorEfectivo).HasColumnType("decimal(12, 2)");
        builder.Property(t => t.TotalPagosNominaEfectivo).HasColumnType("decimal(12, 2)");
        builder.Property(t => t.TotalTransferencias).HasColumnType("decimal(12, 2)");
        builder.Property(t => t.TotalTarjeta).HasColumnType("decimal(12, 2)");

        builder.Property(t => t.NotaCierre).HasMaxLength(300);
        builder.Property(t => t.DetalleConteo).HasMaxLength(1000);

        // Preservar historial: un empleado con turnos no se puede borrar de la base.
        builder.HasOne<Empleado>()
            .WithMany()
            .HasForeignKey(t => t.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<Empleado>()
            .WithMany()
            .HasForeignKey(t => t.CerradoPorEmpleadoId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Un empleado solo puede tener un turno abierto a la vez. El índice es filtrado porque
        // puede tener muchos turnos cerrados: la restricción aplica solo a los abiertos.
        builder.HasIndex(t => t.EmpleadoId)
            .IsUnique()
            .HasFilter("[Estado] = 'Abierto'")
            .HasDatabaseName("IX_TurnosCaja_EmpleadoId_Abierto");

        builder.HasIndex(t => t.FechaApertura);
    }
}
