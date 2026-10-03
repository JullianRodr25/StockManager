namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad Cliente del dominio.
/// Representa un cliente que compra en la ferretería.
/// </summary>
public class Cliente
{
    // "Pwa": se autoregistró desde la PWA pública. "Caja": lo creó un Empleado/Admin desde el
    // panel (típicamente un cliente de mostrador). Puramente informativo (reportes/filtros):
    // no cambia ninguna regla de validación ni de permisos.
    private static readonly string[] OrigenesValidos = { "Pwa", "Caja" };

    // Catálogo simplificado de tipo de documento fiscal (no es el mismo concepto que
    // NumeroIdentificacion, que es el documento de login/cédula): un cliente puede comprar a
    // título personal pero pedir la factura a nombre de una empresa (NIT), por ejemplo.
    private static readonly string[] TiposDocumentoFiscalValidos = { "CC", "NIT", "CE", "Pasaporte", "Otro" };

    public int Id { get; private set; }
    public string NumeroIdentificacion { get; private set; } = null!;  // Cédula, Pasaporte, etc.
    public string Nombre { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string Telefono { get; private set; } = null!;  // Usado para WhatsApp
    public string Direccion { get; private set; } = null!;
    public bool Activo { get; private set; }

    /// <summary>"Pwa" o "Caja" — canal por el que se creó el cliente. Ver OrigenesValidos.</summary>
    public string OrigenRegistro { get; private set; } = null!;

    // --- Datos para solicitar factura electrónica (todos opcionales: un cliente puede no
    // necesitarla nunca). Se guardan acá para no tener que volver a pedirlos en cada venta;
    // VentaService los copia ("snapshot") a la Venta en el momento en que se solicita, así que
    // cambiarlos después no altera facturas ya emitidas.
    public string? TipoDocumentoFiscal { get; private set; }
    public string? NumeroDocumentoFiscal { get; private set; }
    public string? RazonSocialFiscal { get; private set; }
    public string? DireccionFiscal { get; private set; }
    public string? EmailFacturacion { get; private set; }

    /// <summary>
    /// URL pública (Azure Blob Storage) de la foto de perfil del cliente. Null cuando no ha
    /// subido ninguna — la PWA muestra un avatar con sus iniciales en ese caso.
    /// </summary>
    public string? FotoUrl { get; private set; }

    /// <summary>
    /// true cuando hay lo mínimo que exige la DIAN para expedir una factura electrónica
    /// (tipo + número de documento y razón social); dirección y correo son recomendados pero
    /// no bloquean. Lo usa el frontend para decidir si puede ofrecer "Solicitar factura
    /// electrónica" con un clic o si primero hay que pedir esos datos.
    /// </summary>
    public bool TieneDatosFacturacionElectronicaCompletos =>
        !string.IsNullOrWhiteSpace(TipoDocumentoFiscal) &&
        !string.IsNullOrWhiteSpace(NumeroDocumentoFiscal) &&
        !string.IsNullOrWhiteSpace(RazonSocialFiscal);

    private Cliente() { }

    public static Cliente Crear(
        string numeroIdentificacion,
        string nombre,
        string email,
        string passwordHash,
        string telefono,
        string direccion,
        string origenRegistro)
    {
        if (string.IsNullOrWhiteSpace(numeroIdentificacion))
            throw new ArgumentException("El número de identificación no puede estar vacío.", nameof(numeroIdentificacion));

        if (numeroIdentificacion.Length > 50)
            throw new ArgumentException("El número de identificación no puede exceder 50 caracteres.", nameof(numeroIdentificacion));

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del cliente no puede estar vacío.", nameof(nombre));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El email no puede estar vacío.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("El hash de contraseña no puede estar vacío.", nameof(passwordHash));

        if (string.IsNullOrWhiteSpace(telefono))
            throw new ArgumentException("El teléfono no puede estar vacío.", nameof(telefono));

        if (string.IsNullOrWhiteSpace(direccion))
            throw new ArgumentException("La dirección no puede estar vacía.", nameof(direccion));

        if (!OrigenesValidos.Contains(origenRegistro))
            throw new ArgumentException($"El origen de registro '{origenRegistro}' no es válido.", nameof(origenRegistro));

        return new Cliente
        {
            NumeroIdentificacion = numeroIdentificacion.Trim(),
            Nombre = nombre.Trim(),
            Email = email.Trim().ToLower(),
            PasswordHash = passwordHash,
            Telefono = telefono.Trim(),
            Direccion = direccion.Trim(),
            Activo = true,
            OrigenRegistro = origenRegistro
        };
    }

    /// <summary>
    /// Actualiza los datos fiscales usados para solicitar factura electrónica. Todos
    /// opcionales (null/vacío limpia el campo); si se deja vacío el tipo, número o razón
    /// social, TieneDatosFacturacionElectronicaCompletos vuelve a dar false. No valida el
    /// número de documento contra un formato específico (el NIT colombiano incluye dígito de
    /// verificación con reglas propias que no vale la pena duplicar acá).
    /// </summary>
    public void ActualizarDatosFacturacionElectronica(
        string? tipoDocumentoFiscal,
        string? numeroDocumentoFiscal,
        string? razonSocialFiscal,
        string? direccionFiscal,
        string? emailFacturacion)
    {
        if (!string.IsNullOrWhiteSpace(tipoDocumentoFiscal) && !TiposDocumentoFiscalValidos.Contains(tipoDocumentoFiscal))
            throw new ArgumentException($"El tipo de documento fiscal '{tipoDocumentoFiscal}' no es válido.", nameof(tipoDocumentoFiscal));

        TipoDocumentoFiscal = string.IsNullOrWhiteSpace(tipoDocumentoFiscal) ? null : tipoDocumentoFiscal.Trim();
        NumeroDocumentoFiscal = string.IsNullOrWhiteSpace(numeroDocumentoFiscal) ? null : numeroDocumentoFiscal.Trim();
        RazonSocialFiscal = string.IsNullOrWhiteSpace(razonSocialFiscal) ? null : razonSocialFiscal.Trim();
        DireccionFiscal = string.IsNullOrWhiteSpace(direccionFiscal) ? null : direccionFiscal.Trim();
        EmailFacturacion = string.IsNullOrWhiteSpace(emailFacturacion) ? null : emailFacturacion.Trim();
    }

    /// <summary>
    /// Actualiza los datos de contacto del cliente. NO permite cambiar NumeroIdentificacion
    /// (es su identificador de login, igual que la cédula de un Empleado) ni PasswordHash
    /// (eso requeriría un flujo de cambio de contraseña aparte, con su propia validación).
    /// </summary>
    public void ActualizarInformacion(string nombre, string email, string telefono, string direccion)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del cliente no puede estar vacío.", nameof(nombre));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El email no puede estar vacío.", nameof(email));

        if (string.IsNullOrWhiteSpace(telefono))
            throw new ArgumentException("El teléfono no puede estar vacío.", nameof(telefono));

        if (string.IsNullOrWhiteSpace(direccion))
            throw new ArgumentException("La dirección no puede estar vacía.", nameof(direccion));

        Nombre = nombre.Trim();
        Email = email.Trim().ToLower();
        Telefono = telefono.Trim();
        Direccion = direccion.Trim();
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;

    /// <summary>
    /// Reemplaza la foto de perfil. Quien llama es responsable de haber subido el archivo a
    /// Azure Blob Storage antes y de borrar (mejor esfuerzo) el blob anterior — esta entidad
    /// solo guarda la URL resultante, igual que ProductoFoto.Url.
    /// </summary>
    public void ActualizarFoto(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("La URL de la foto no puede estar vacía.", nameof(url));

        FotoUrl = url;
    }

    /// <summary>Quita la foto de perfil (vuelve al avatar con iniciales en la PWA).</summary>
    public void EliminarFoto() => FotoUrl = null;

    /// <summary>
    /// Reemplaza el hash de contraseña del cliente. Usado por el flujo de recuperación por
    /// correo (token firmado, ver IPasswordResetTokenService). La validación de la contraseña
    /// en texto plano (longitud, etc.) ocurre antes, en la capa de aplicación; aquí solo se
    /// persiste el hash ya calculado.
    /// </summary>
    public void ActualizarPasswordHash(string nuevoHash)
    {
        if (string.IsNullOrWhiteSpace(nuevoHash))
            throw new ArgumentException("El hash de contraseña no puede estar vacío.", nameof(nuevoHash));

        PasswordHash = nuevoHash;
    }
}
