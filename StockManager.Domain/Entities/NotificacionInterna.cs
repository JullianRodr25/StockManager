namespace StockManager.Domain.Entities;

/// <summary>
/// Notificación interna para el personal de la tienda (la "campana" del panel), distinta de
/// NotificacionLog (que registra envíos de WhatsApp a clientes/proveedores). Se genera cuando
/// pasa algo que un empleado debería revisar: un producto entra en stock bajo, llega un
/// pedido nuevo desde la PWA, o una cuenta por pagar está por vencer.
/// </summary>
public class NotificacionInterna
{
    /// <summary>
    /// Tipos válidos de notificación interna. Determinan qué icono/acción "ir a ver" usa el
    /// frontend, en conjunto con EntidadTipo/EntidadId.
    /// </summary>
    public static readonly string[] TiposValidos = { "StockBajo", "PedidoNuevo", "CuentaPorPagarProximaAVencer" };

    public int Id { get; private set; }
    public string Tipo { get; private set; } = null!;
    public string Titulo { get; private set; } = null!;
    public string Mensaje { get; private set; } = null!;

    /// <summary>
    /// Qué entidad de negocio originó la notificación (ej. "Producto", "Pedido",
    /// "CuentaPorPagar") y su Id, para que el botón "Ir a ver" del frontend pueda navegar
    /// directo a ella sin tener que parsear el mensaje.
    /// </summary>
    public string EntidadTipo { get; private set; } = null!;
    public int EntidadId { get; private set; }

    public DateTime FechaCreacion { get; private set; }
    public bool Leida { get; private set; }
    public DateTime? FechaLeida { get; private set; }

    private NotificacionInterna() { }

    public static NotificacionInterna Crear(string tipo, string titulo, string mensaje, string entidadTipo, int entidadId)
    {
        if (!TiposValidos.Contains(tipo))
            throw new ArgumentException($"Tipo de notificación inválido: '{tipo}'.", nameof(tipo));

        if (string.IsNullOrWhiteSpace(titulo))
            throw new ArgumentException("El título no puede estar vacío.", nameof(titulo));

        if (string.IsNullOrWhiteSpace(mensaje))
            throw new ArgumentException("El mensaje no puede estar vacío.", nameof(mensaje));

        if (string.IsNullOrWhiteSpace(entidadTipo))
            throw new ArgumentException("EntidadTipo no puede estar vacío.", nameof(entidadTipo));

        if (entidadId <= 0)
            throw new ArgumentException("EntidadId debe ser mayor a 0.", nameof(entidadId));

        return new NotificacionInterna
        {
            Tipo = tipo,
            Titulo = titulo.Trim(),
            Mensaje = mensaje.Trim(),
            EntidadTipo = entidadTipo,
            EntidadId = entidadId,
            FechaCreacion = DateTime.UtcNow,
            Leida = false
        };
    }

    public void MarcarLeida()
    {
        if (Leida)
            return;

        Leida = true;
        FechaLeida = DateTime.UtcNow;
    }
}
