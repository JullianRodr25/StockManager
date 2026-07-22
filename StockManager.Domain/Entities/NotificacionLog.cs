namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad NotificacionLog del dominio.
/// Trazabilidad de todos los envíos de notificaciones (WhatsApp, Push, Email).
/// </summary>
public class NotificacionLog
{
    public int Id { get; private set; }
    public string Canal { get; private set; } = null!;  // 'WhatsApp' | 'Push' | 'Email'
    public string Destinatario { get; private set; } = null!;
    public string ReferenciaTipo { get; private set; } = null!;  // 'Venta' | 'Pedido' | 'BackorderRequest'
    public int ReferenciaId { get; private set; }
    public string Estado { get; private set; } = null!;  // 'Enviado' | 'Fallido'
    public DateTime FechaEnvio { get; private set; }
    public string? DetalleError { get; private set; }

    private NotificacionLog() { }

    public static NotificacionLog Crear(
        string canal,
        string destinatario,
        string referenciaTipo,
        int referenciaId,
        string estado,
        string? detalleError = null)
    {
        var canales = new[] { "WhatsApp", "Push", "Email" };
        if (!canales.Contains(canal))
            throw new ArgumentException($"El canal '{canal}' no es válido.", nameof(canal));

        if (string.IsNullOrWhiteSpace(destinatario))
            throw new ArgumentException("El destinatario no puede estar vacío.", nameof(destinatario));

        if (string.IsNullOrWhiteSpace(referenciaTipo))
            throw new ArgumentException("El tipo de referencia no puede estar vacío.", nameof(referenciaTipo));

        if (referenciaId <= 0)
            throw new ArgumentException("ReferenciaId debe ser mayor a 0.", nameof(referenciaId));

        var estados = new[] { "Enviado", "Fallido" };
        if (!estados.Contains(estado))
            throw new ArgumentException($"El estado '{estado}' no es válido.", nameof(estado));

        return new NotificacionLog
        {
            Canal = canal,
            Destinatario = destinatario.Trim(),
            ReferenciaTipo = referenciaTipo,
            ReferenciaId = referenciaId,
            Estado = estado,
            FechaEnvio = DateTime.UtcNow,
            DetalleError = detalleError
        };
    }
}
