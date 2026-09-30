using System.Text.RegularExpressions;

namespace StockManager.Domain.Entities;

public class Configuracion
{
    // Formato internacional E.164: '+' seguido de 8 a 15 dígitos, sin espacios ni separadores.
    private static readonly Regex FormatoTelefonoE164 = new(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled);

    public int Id { get; private set; }
    public decimal TarifaIvaPorDefecto { get; private set; }

    /// <summary>
    /// Número de WhatsApp (formato E.164) al que se envían las alertas administrativas
    /// (pedidos nuevos, cuentas por pagar próximas a vencer). Null o vacío desactiva ese
    /// aviso sin afectar el resto de notificaciones por WhatsApp. Se guarda en base de datos
    /// (y no en appsettings) porque es un dato de negocio que un Admin debe poder cambiar
    /// desde la pantalla de Configuración sin necesidad de un despliegue.
    /// </summary>
    public string? TelefonoNotificacionesAdmin { get; private set; }

    /// <summary>
    /// Nombre exacto (tal como lo expone el sistema operativo/driver) de la impresora térmica
    /// de tiquetes conectada al computador del mostrador, usada por QZ Tray para imprimir la
    /// factura y para enviar la orden de apertura del cajón de dinero. Se guarda en base de
    /// datos porque es un dato del equipo físico de cada sucursal, no algo fijo en el código.
    /// </summary>
    public string? NombreImpresoraTickets { get; private set; }

    private Configuracion() { }

    public static Configuracion Crear(
        decimal tarifaIvaPorDefecto,
        string? telefonoNotificacionesAdmin = null,
        string? nombreImpresoraTickets = null)
    {
        if (tarifaIvaPorDefecto < 0 || tarifaIvaPorDefecto > 100)
            throw new ArgumentException("La tarifa de IVA debe estar entre 0 y 100.");

        var configuracion = new Configuracion { TarifaIvaPorDefecto = tarifaIvaPorDefecto };
        configuracion.ActualizarTelefonoNotificacionesAdmin(telefonoNotificacionesAdmin);
        configuracion.ActualizarNombreImpresoraTickets(nombreImpresoraTickets);
        return configuracion;
    }

    public void ActualizarTarifaIva(decimal nuevaTarifa)
    {
        if (nuevaTarifa < 0 || nuevaTarifa > 100)
            throw new ArgumentException("La tarifa de IVA debe estar entre 0 y 100.");

        TarifaIvaPorDefecto = nuevaTarifa;
    }

    public void ActualizarTelefonoNotificacionesAdmin(string? nuevoTelefono)
    {
        var telefono = string.IsNullOrWhiteSpace(nuevoTelefono) ? null : nuevoTelefono.Trim();

        if (telefono is not null && !FormatoTelefonoE164.IsMatch(telefono))
            throw new ArgumentException("El teléfono debe estar en formato internacional E.164, ej. +573001234567.");

        TelefonoNotificacionesAdmin = telefono;
    }

    public void ActualizarNombreImpresoraTickets(string? nuevoNombre)
    {
        NombreImpresoraTickets = string.IsNullOrWhiteSpace(nuevoNombre) ? null : nuevoNombre.Trim();
    }
}