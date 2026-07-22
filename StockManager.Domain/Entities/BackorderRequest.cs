namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad BackorderRequest del dominio.
/// Representa una solicitud de notificación cuando un producto vuelva a estar disponible.
/// </summary>
public class BackorderRequest
{
    public int Id { get; private set; }
    public int ClienteId { get; private set; }
    public int ProductoId { get; private set; }
    public int CantidadDeseada { get; private set; }
    public DateTime FechaSolicitud { get; private set; }
    public string Estado { get; private set; } = null!;  // 'Pendiente' | 'Notificado' | 'Cancelado'
    public DateTime? FechaNotificacion { get; private set; }

    private BackorderRequest() { }

    public static BackorderRequest Crear(int clienteId, int productoId, int cantidadDeseada)
    {
        if (clienteId <= 0)
            throw new ArgumentException("ClienteId debe ser mayor a 0.", nameof(clienteId));

        if (productoId <= 0)
            throw new ArgumentException("ProductoId debe ser mayor a 0.", nameof(productoId));

        if (cantidadDeseada <= 0)
            throw new ArgumentException("La cantidad deseada debe ser mayor a 0.", nameof(cantidadDeseada));

        return new BackorderRequest
        {
            ClienteId = clienteId,
            ProductoId = productoId,
            CantidadDeseada = cantidadDeseada,
            FechaSolicitud = DateTime.UtcNow,
            Estado = "Pendiente"
        };
    }

    public void Notificar()
    {
        if (Estado != "Pendiente")
            throw new InvalidOperationException("Solo se puede notificar un BackorderRequest en estado 'Pendiente'.");

        Estado = "Notificado";
        FechaNotificacion = DateTime.UtcNow;
    }

    public void Cancelar()
    {
        Estado = "Cancelado";
    }
}
