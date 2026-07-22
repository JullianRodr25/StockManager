namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad Pedido del dominio.
/// Representa un pedido realizado vía PWA para entrega a domicilio.
/// </summary>
public class Pedido
{
    public int Id { get; private set; }
    public int ClienteId { get; private set; }
    public DateTime Fecha { get; private set; }
    public string Estado { get; private set; } = null!;  // Pendiente | Confirmado | EnPreparacion | EnCamino | Entregado | Cancelado
    public string Direccion { get; private set; } = null!;

    private Pedido() { }

    public static Pedido Crear(int clienteId, string direccion)
    {
        if (clienteId <= 0)
            throw new ArgumentException("ClienteId debe ser mayor a 0.", nameof(clienteId));

        if (string.IsNullOrWhiteSpace(direccion))
            throw new ArgumentException("La dirección no puede estar vacía.", nameof(direccion));

        return new Pedido
        {
            ClienteId = clienteId,
            Fecha = DateTime.UtcNow,
            Estado = "Pendiente",
            Direccion = direccion.Trim()
        };
    }

    public void CambiarEstado(string nuevoEstado)
    {
        var estadosValidos = new[] { "Pendiente", "Confirmado", "EnPreparacion", "EnCamino", "Entregado", "Cancelado" };
        if (!estadosValidos.Contains(nuevoEstado))
            throw new ArgumentException($"El estado '{nuevoEstado}' no es válido.", nameof(nuevoEstado));

        Estado = nuevoEstado;
    }
}
