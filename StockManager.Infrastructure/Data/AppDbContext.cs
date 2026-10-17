using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StockManager.Domain.Entities;
using StockManager.Infrastructure.Data.Configurations;

namespace StockManager.Infrastructure.Data;

/// <summary>
/// DbContext principal de la aplicación StockManager.
/// Centraliza todas las entidades y configuraciones de mapeo.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // DbSets para todas las entidades
    public DbSet<Categoria> Categorias { get; set; } = null!;
    public DbSet<Producto> Productos { get; set; } = null!;
    public DbSet<ProductoFoto> ProductoFotos { get; set; } = null!;
    public DbSet<Configuracion> Configuraciones { get; set; } = null!;
    public DbSet<Empleado> Empleados { get; set; } = null!;
    public DbSet<Cliente> Clientes { get; set; } = null!;
    public DbSet<Venta> Ventas { get; set; } = null!;
    public DbSet<DetalleVenta> DetallesVenta { get; set; } = null!;
    public DbSet<AbonoCuenta> AbonosCuenta { get; set; } = null!;
    public DbSet<DetallePagoVenta> DetallesPagoVenta { get; set; } = null!;
    public DbSet<Pedido> Pedidos { get; set; } = null!;
    public DbSet<DetallePedido> DetallesPedido { get; set; } = null!;
    public DbSet<MovimientoStock> MovimientosStock { get; set; } = null!;
    public DbSet<BackorderRequest> BackorderRequests { get; set; } = null!;
    public DbSet<Factura> Facturas { get; set; } = null!;
    public DbSet<NotificacionLog> NotificacionesLog { get; set; } = null!;
    public DbSet<Proveedor> Proveedores { get; set; } = null!;
    public DbSet<CuentaPorPagar> CuentasPorPagar { get; set; } = null!;
    public DbSet<AbonoCuentaPorPagar> AbonosCuentaPorPagar { get; set; } = null!;
    public DbSet<NotificacionInterna> NotificacionesInternas { get; set; } = null!;
    public DbSet<ResenaProducto> ResenasProducto { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar todas las configuraciones
        modelBuilder.ApplyConfiguration(new CategoriaConfiguration());
        modelBuilder.ApplyConfiguration(new ProductoConfiguration());
        modelBuilder.ApplyConfiguration(new ProductoFotoConfiguration());
        modelBuilder.ApplyConfiguration(new ConfiguracionConfiguration());
        modelBuilder.ApplyConfiguration(new EmpleadoConfiguration());
        modelBuilder.ApplyConfiguration(new ClienteConfiguration());
        modelBuilder.ApplyConfiguration(new VentaConfiguration());
        modelBuilder.ApplyConfiguration(new DetalleVentaConfiguration());
        modelBuilder.ApplyConfiguration(new AbonoCuentaConfiguration());
        modelBuilder.ApplyConfiguration(new DetallePagoVentaConfiguration());
        modelBuilder.ApplyConfiguration(new PedidoConfiguration());
        modelBuilder.ApplyConfiguration(new DetallePedidoConfiguration());
        modelBuilder.ApplyConfiguration(new MovimientoStockConfiguration());
        modelBuilder.ApplyConfiguration(new BackorderRequestConfiguration());
        modelBuilder.ApplyConfiguration(new FacturaConfiguration());
        modelBuilder.ApplyConfiguration(new NotificacionLogConfiguration());
        modelBuilder.ApplyConfiguration(new ProveedorConfiguration());
        modelBuilder.ApplyConfiguration(new CuentaPorPagarConfiguration());
        modelBuilder.ApplyConfiguration(new AbonoCuentaPorPagarConfiguration());
        modelBuilder.ApplyConfiguration(new NotificacionInternaConfiguration());
        modelBuilder.ApplyConfiguration(new ResenaProductoConfiguration());

        MarcarInstantesComoUtc(modelBuilder);
    }

    // Campos que son un DÍA de calendario (no un instante): no se marcan como UTC, porque
    // "2026-10-20T00:00:00Z" se vería como el 19 en Colombia.
    private static readonly HashSet<string> CamposSoloFecha =
    [
        nameof(CuentaPorPagar.FechaVencimiento),
        nameof(Configuracion.ResolucionDianFecha),
    ];

    /// <summary>
    /// SQL Server devuelve los datetime2 con Kind = Unspecified, y System.Text.Json los
    /// serializa SIN la "Z" final; el navegador entonces los lee como hora local y la factura
    /// aparece corrida 5 horas. Todas las fechas se guardan en UTC, así que al leerlas se
    /// marcan como tales y la API las envía con "Z". No cambia el esquema (no hay migración).
    /// </summary>
    private static void MarcarInstantesComoUtc(ModelBuilder modelBuilder)
    {
        var instante = new ValueConverter<DateTime, DateTime>(
            v => v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        var instanteOpcional = new ValueConverter<DateTime?, DateTime?>(
            v => v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entidad in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var propiedad in entidad.GetProperties())
            {
                if (CamposSoloFecha.Contains(propiedad.Name) || propiedad.GetValueConverter() is not null)
                    continue;

                if (propiedad.ClrType == typeof(DateTime))
                    propiedad.SetValueConverter(instante);
                else if (propiedad.ClrType == typeof(DateTime?))
                    propiedad.SetValueConverter(instanteOpcional);
            }
        }
    }
}
