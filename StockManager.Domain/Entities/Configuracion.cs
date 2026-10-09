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

    // --- Datos del emisor (negocio) que se imprimen en el encabezado del tiquete físico y de
    // la factura. Antes estaban escritos fijos en el código (impresionService.ts); viven acá
    // para que un Admin los pueda corregir desde la pantalla de Configuración sin desplegar
    // un cambio de código (ej. si cambia el NIT, la dirección o el teléfono del negocio).
    public string? NombreEmpresa { get; private set; }
    public string? NitEmpresa { get; private set; }
    public string? DireccionEmpresa { get; private set; }
    public string? TelefonoEmpresa { get; private set; }
    public string? EmailEmpresa { get; private set; }

    // --- Datos de facturación que se imprimen en el tiquete y en la factura (todos opcionales y
    // editables desde Configuración). Las líneas de la resolución solo salen impresas si el negocio
    // las configura; el sistema no inventa ni asume datos tributarios.
    public string? CiudadEmpresa { get; private set; }
    public string? BarrioEmpresa { get; private set; }

    /// <summary>Ej. "Responsable de IVA" o "No responsable de IVA".</summary>
    public string? ResponsabilidadIvaEmpresa { get; private set; }

    /// <summary>Código de actividad económica (CIIU), ej. "4752".</summary>
    public string? ActividadEconomicaEmpresa { get; private set; }

    public string? ResolucionDianNumero { get; private set; }
    public DateTime? ResolucionDianFecha { get; private set; }
    public string? ResolucionDianPrefijo { get; private set; }
    public int? ResolucionDianRangoDesde { get; private set; }
    public int? ResolucionDianRangoHasta { get; private set; }
    public int? ResolucionDianVigenciaMeses { get; private set; }

    /// <summary>Texto legal al pie de la factura (ej. mención de la letra de cambio, Ley 1231 de 2008).</summary>
    public string? TextoLegalFactura { get; private set; }

    /// <summary>Política de cambios y devoluciones al pie de la factura.</summary>
    public string? PoliticaCambiosFactura { get; private set; }

    private Configuracion() { }

    public static Configuracion Crear(
        decimal tarifaIvaPorDefecto,
        string? telefonoNotificacionesAdmin = null,
        string? nombreImpresoraTickets = null,
        string? nombreEmpresa = null,
        string? nitEmpresa = null,
        string? direccionEmpresa = null,
        string? telefonoEmpresa = null,
        string? emailEmpresa = null)
    {
        if (tarifaIvaPorDefecto < 0 || tarifaIvaPorDefecto > 100)
            throw new ArgumentException("La tarifa de IVA debe estar entre 0 y 100.");

        var configuracion = new Configuracion { TarifaIvaPorDefecto = tarifaIvaPorDefecto };
        configuracion.ActualizarTelefonoNotificacionesAdmin(telefonoNotificacionesAdmin);
        configuracion.ActualizarNombreImpresoraTickets(nombreImpresoraTickets);
        configuracion.ActualizarDatosEmpresa(nombreEmpresa, nitEmpresa, direccionEmpresa, telefonoEmpresa, emailEmpresa);
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

    /// <summary>
    /// Actualiza los datos del emisor (negocio) que se imprimen en el tiquete físico y en la
    /// factura. Todos son opcionales (texto libre, sin validación de formato) porque son datos
    /// puramente informativos: a diferencia del teléfono de notificaciones, acá no hay un
    /// formato único que validar (el NIT colombiano y la dirección no siguen un patrón fijo).
    /// </summary>
    public void ActualizarDatosEmpresa(
        string? nombreEmpresa,
        string? nitEmpresa,
        string? direccionEmpresa,
        string? telefonoEmpresa,
        string? emailEmpresa)
    {
        NombreEmpresa = string.IsNullOrWhiteSpace(nombreEmpresa) ? null : nombreEmpresa.Trim();
        NitEmpresa = string.IsNullOrWhiteSpace(nitEmpresa) ? null : nitEmpresa.Trim();
        DireccionEmpresa = string.IsNullOrWhiteSpace(direccionEmpresa) ? null : direccionEmpresa.Trim();
        TelefonoEmpresa = string.IsNullOrWhiteSpace(telefonoEmpresa) ? null : telefonoEmpresa.Trim();
        EmailEmpresa = string.IsNullOrWhiteSpace(emailEmpresa) ? null : emailEmpresa.Trim();
    }

    /// <summary>
    /// Actualiza los datos de facturación (ciudad, régimen, resolución y textos al pie). Todos son
    /// opcionales; lo único que se valida es la coherencia interna: el rango autorizado debe
    /// tener ambos extremos y "desde" no puede superar a "hasta", y la vigencia debe ser positiva.
    /// </summary>
    public void ActualizarDatosFacturacion(
        string? ciudad,
        string? barrio,
        string? responsabilidadIva,
        string? actividadEconomica,
        string? resolucionNumero,
        DateTime? resolucionFecha,
        string? resolucionPrefijo,
        int? rangoDesde,
        int? rangoHasta,
        int? vigenciaMeses,
        string? textoLegal,
        string? politicaCambios)
    {
        if (rangoDesde.HasValue != rangoHasta.HasValue)
            throw new ArgumentException("El rango autorizado debe tener el número inicial y el final.");

        if (rangoDesde.HasValue && (rangoDesde.Value < 1 || rangoHasta!.Value < rangoDesde.Value))
            throw new ArgumentException("El rango autorizado no es válido: el inicial debe ser mayor a 0 y no puede superar al final.");

        if (vigenciaMeses.HasValue && (vigenciaMeses.Value < 1 || vigenciaMeses.Value > 120))
            throw new ArgumentException("La vigencia de la resolución debe estar entre 1 y 120 meses.");

        CiudadEmpresa = Limpiar(ciudad);
        BarrioEmpresa = Limpiar(barrio);
        ResponsabilidadIvaEmpresa = Limpiar(responsabilidadIva);
        ActividadEconomicaEmpresa = Limpiar(actividadEconomica);
        ResolucionDianNumero = Limpiar(resolucionNumero);
        ResolucionDianFecha = resolucionFecha?.Date;
        ResolucionDianPrefijo = Limpiar(resolucionPrefijo);
        ResolucionDianRangoDesde = rangoDesde;
        ResolucionDianRangoHasta = rangoHasta;
        ResolucionDianVigenciaMeses = vigenciaMeses;
        TextoLegalFactura = Limpiar(textoLegal);
        PoliticaCambiosFactura = Limpiar(politicaCambios);
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
