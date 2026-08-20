using Microsoft.EntityFrameworkCore;
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
    public DbSet<Configuracion> Configuraciones { get; set; } = null!;
    public DbSet<Empleado> Empleados { get; set; } = null!;
    public DbSet<Cliente> Clientes { get; set; } = null!;
    public DbSet<Venta> Ventas { get; set; } = null!;
    public DbSet<DetalleVenta> DetallesVenta { get; set; } = null!;
    public DbSet<AbonoCuenta> AbonosCuenta { get; set; } = null!;
    public DbSet<Pedido> Pedidos { get; set; } = null!;
    public DbSet<DetallePedido> DetallesPedido { get; set; } = null!;
    public DbSet<MovimientoStock> MovimientosStock { get; set; } = null!;
    public DbSet<BackorderRequest> BackorderRequests { get; set; } = null!;
    public DbSet<Factura> Facturas { get; set; } = null!;
    public DbSet<NotificacionLog> NotificacionesLog { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar todas las configuraciones
        modelBuilder.ApplyConfiguration(new CategoriaConfiguration());
        modelBuilder.ApplyConfiguration(new ProductoConfiguration());
        modelBuilder.ApplyConfiguration(new ConfiguracionConfiguration());
        modelBuilder.ApplyConfiguration(new EmpleadoConfiguration());
        modelBuilder.ApplyConfiguration(new ClienteConfiguration());
        modelBuilder.ApplyConfiguration(new VentaConfiguration());
        modelBuilder.ApplyConfiguration(new DetalleVentaConfiguration());
        modelBuilder.ApplyConfiguration(new AbonoCuentaConfiguration());
        modelBuilder.ApplyConfiguration(new PedidoConfiguration());
        modelBuilder.ApplyConfiguration(new DetallePedidoConfiguration());
        modelBuilder.ApplyConfiguration(new MovimientoStockConfiguration());
        modelBuilder.ApplyConfiguration(new BackorderRequestConfiguration());
        modelBuilder.ApplyConfiguration(new FacturaConfiguration());
        modelBuilder.ApplyConfiguration(new NotificacionLogConfiguration());
    }
}
