using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Excel;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IClienteExcelImportador"/> para el Excel de terceros del programa
/// contable anterior.
///
/// Decisiones de diseño:
/// - Las reglas del cliente (campos obligatorios, tipos de documento fiscal válidos, longitudes
///   lógicas) NO se reescriben aquí: se usa Cliente.Crear y ActualizarDatosFacturacionElectronica,
///   los mismos que usa el formulario. Aquí solo viven las reglas que dependen del archivo o del
///   resto de clientes: cédula repetida, correo repetido, categoría y cómo se arma cada campo.
/// - Es una migración, no una sincronización: un cliente que ya existe NO se modifica (se avisa y
///   se omite). Así se puede volver a subir el archivo, tras corregir errores, sin duplicar ni
///   pisar lo que ya se editó a mano.
/// - Un correo repetido o mal escrito no frena al cliente: se importa sin correo y se avisa. La
///   cédula (que es su usuario de login) sí es obligatoria y única.
/// - A nadie se le entrega contraseña: cada cliente recibe una aleatoria que nadie conoce, y entra
///   con "Olvidé mi contraseña" (si tiene correo) o con una que le asigne el administrador.
/// - Los datos fiscales se llenan con lo que ya trae el archivo (tipo y número de documento,
///   razón social, correo), para que el cliente quede listo para factura electrónica.
/// </summary>
public class ClienteExcelImportador : IClienteExcelImportador
{
    private const int MaxNombre = 150;
    private const int MaxDireccion = 300;
    private const int MaxTelefono = 20;
    private const int MaxEmail = 200;
    private const int MaxNumeroIdentificacion = 50;
    private const int MaxNumeroFiscal = 30;
    private const int MinDigitosTelefono = 7;

    // Hash de relleno para la vista previa: no se guarda nunca, y evita gastar CPU hasheando.
    private const string HashVistaPrevia = "vista-previa";

    private static readonly Regex FormatoEmail = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private readonly AppDbContext _dbContext;
    private readonly PasswordHasher<Cliente> _passwordHasher = new();

    public ClienteExcelImportador(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Error "esperado" de una fila (dato inválido, regla incumplida): se informa al usuario. Otra
    // excepción es un fallo real del servidor y se deja propagar.
    private sealed class ErrorFilaImportacion : Exception
    {
        public ErrorFilaImportacion(string mensaje) : base(mensaje) { }
    }

    public async Task<ImportarClientesResponse> ProcesarAsync(Stream archivo, bool aplicarCambios)
    {
        XLWorkbook libro;
        try
        {
            libro = new XLWorkbook(archivo);
        }
        catch (Exception)
        {
            throw new InvalidOperationException("No se pudo leer el archivo. Verifica que sea un Excel (.xlsx) válido.");
        }

        using (libro)
        {
            var hoja = libro.Worksheets.FirstOrDefault()
                       ?? throw new InvalidOperationException("El archivo no contiene hojas de cálculo.");

            var columnas = LeerEncabezados(hoja);

            // Solo cuentan filas con contenido real (el formato de celdas vacías no es una fila).
            var ultimaFila = hoja.LastRowUsed(XLCellsUsedOptions.Contents)?.RowNumber() ?? ClienteExcelFormato.FilaEncabezado;
            if (ultimaFila < ClienteExcelFormato.PrimeraFilaDatos)
                throw new InvalidOperationException("El archivo no contiene filas de clientes.");

            return await ProcesarFilasAsync(hoja, columnas, ultimaFila, aplicarCambios);
        }
    }

    // ---------------------------------------------------------------------------------------
    // Encabezados
    // ---------------------------------------------------------------------------------------

    // Nombre del encabezado (mayúsculas, sin espacios sobrantes) -> número de columna.
    private static Dictionary<string, int> LeerEncabezados(IXLWorksheet hoja)
    {
        var columnas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var ultimaColumna = hoja.Row(ClienteExcelFormato.FilaEncabezado).LastCellUsed()?.Address.ColumnNumber ?? 0;
        for (var columna = 1; columna <= ultimaColumna; columna++)
        {
            var nombre = hoja.Cell(ClienteExcelFormato.FilaEncabezado, columna).GetString().Trim();
            if (nombre.Length > 0)
                columnas.TryAdd(nombre, columna);
        }

        var faltantes = ClienteExcelFormato.EncabezadosObligatorios.Where(e => !columnas.ContainsKey(e)).ToList();
        if (faltantes.Count > 0)
        {
            throw new InvalidOperationException(
                $"El archivo no tiene el formato esperado: faltan las columnas {string.Join(", ", faltantes)} en la fila 1. " +
                "Usa el Excel de terceros exportado del programa contable sin cambiar los encabezados.");
        }

        return columnas;
    }

    // ---------------------------------------------------------------------------------------
    // Filas
    // ---------------------------------------------------------------------------------------

    private async Task<ImportarClientesResponse> ProcesarFilasAsync(
        IXLWorksheet hoja,
        Dictionary<string, int> columnas,
        int ultimaFila,
        bool aplicarCambios)
    {
        var respuesta = new ImportarClientesResponse();

        // Datos de referencia, cargados una sola vez.
        var numerosExistentes = new HashSet<string>(
            await _dbContext.Clientes.AsNoTracking().Select(c => c.NumeroIdentificacion).ToListAsync(),
            StringComparer.OrdinalIgnoreCase);

        // El correo es único entre clientes y empleados (la recuperación de contraseña busca en ambos).
        var emailsEnUso = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var email in await _dbContext.Clientes.AsNoTracking().Where(c => c.Email != null).Select(c => c.Email!).ToListAsync())
            emailsEnUso.Add(email);
        foreach (var email in await _dbContext.Empleados.AsNoTracking().Select(e => e.Email).ToListAsync())
            emailsEnUso.Add(email);

        var numerosVistosEnArchivo = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // numero -> fila
        var nuevos = new List<Cliente>();

        for (var numeroFila = ClienteExcelFormato.PrimeraFilaDatos; numeroFila <= ultimaFila; numeroFila++)
        {
            if (FilaVacia(hoja, columnas, numeroFila))
                continue;

            respuesta.TotalFilas++;

            try
            {
                string? Texto(string columna) => LeerTexto(hoja, columnas, numeroFila, columna);

                // 1) ¿Es un cliente? El programa contable también guarda nómina, contabilidad, etc.
                var categoria = Texto(ClienteExcelFormato.ColCategoria);
                if (categoria is not null
                    && !string.Equals(categoria, ClienteExcelFormato.CategoriaClientes, StringComparison.OrdinalIgnoreCase))
                {
                    respuesta.OtraCategoria++;
                    respuesta.Avisos.Add(Aviso(numeroFila, $"No se importó: la categoría es '{categoria}', no '{ClienteExcelFormato.CategoriaClientes}'."));
                    continue;
                }

                // 2) Identificación: es el usuario de login, así que es obligatoria y única.
                var numero = LimpiarIdentificacion(Texto(ClienteExcelFormato.ColIdentificacion))
                    ?? throw new ErrorFilaImportacion("La identificación es obligatoria.");
                if (numero.Length > MaxNumeroIdentificacion)
                    throw new ErrorFilaImportacion($"La identificación no puede exceder {MaxNumeroIdentificacion} caracteres.");

                if (numerosVistosEnArchivo.TryGetValue(numero, out var filaPrevia))
                    throw new ErrorFilaImportacion($"La identificación {numero} está repetida en el archivo (ya aparece en la fila {filaPrevia}).");
                numerosVistosEnArchivo[numero] = numeroFila;

                if (numerosExistentes.Contains(numero))
                {
                    respuesta.YaExistentes++;
                    respuesta.Avisos.Add(Aviso(numeroFila, $"Ya existe un cliente con la identificación {numero}: se dejó como estaba."));
                    continue;
                }

                // 3) Nombre y datos de contacto.
                var tipoPersona = Texto(ClienteExcelFormato.ColTipoPersona);
                var nombre = ArmarNombre(
                    tipoPersona,
                    Texto(ClienteExcelFormato.ColRazonSocial),
                    Texto(ClienteExcelFormato.ColNombre),
                    Texto(ClienteExcelFormato.ColOtrosNombres),
                    Texto(ClienteExcelFormato.ColApellido),
                    Texto(ClienteExcelFormato.ColSegundoApellido));
                if (nombre.Length > MaxNombre)
                    throw new ErrorFilaImportacion($"El nombre supera los {MaxNombre} caracteres.");

                var telefono = NormalizarTelefono(Texto(ClienteExcelFormato.ColTelefono), numeroFila, respuesta);
                var direccion = Texto(ClienteExcelFormato.ColDireccion) ?? ClienteExcelFormato.TextoSinDato;
                if (direccion.Length > MaxDireccion)
                    throw new ErrorFilaImportacion($"La dirección supera los {MaxDireccion} caracteres.");

                var email = ResolverEmail(Texto(ClienteExcelFormato.ColCorreo), numeroFila, emailsEnUso, respuesta);

                // 4) Datos fiscales a partir de lo que ya trae el archivo.
                var digito = Texto(ClienteExcelFormato.ColDigitoVerificacion);
                var numeroFiscal = digito is null ? numero : $"{numero}-{digito}";
                if (numeroFiscal.Length > MaxNumeroFiscal)
                    throw new ErrorFilaImportacion($"El número de documento fiscal supera los {MaxNumeroFiscal} caracteres.");

                // 5) Reglas del dominio: las mismas del formulario.
                var cliente = Cliente.Crear(
                    numeroIdentificacion: numero,
                    nombre: nombre,
                    email: email,
                    passwordHash: aplicarCambios ? GenerarHashAleatorio() : HashVistaPrevia,
                    telefono: telefono,
                    direccion: direccion,
                    origenRegistro: "Caja");

                cliente.ActualizarDatosFacturacionElectronica(
                    tipoDocumentoFiscal: MapearTipoDocumento(Texto(ClienteExcelFormato.ColTipoIdentificacion)),
                    numeroDocumentoFiscal: numeroFiscal,
                    razonSocialFiscal: nombre,
                    direccionFiscal: null,
                    emailFacturacion: email);

                nuevos.Add(cliente);
                respuesta.Creados++;
            }
            catch (Exception ex) when (ex is ErrorFilaImportacion or ArgumentException or DomainException)
            {
                respuesta.Errores.Add(new ErrorImportacion { Fila = numeroFila, Mensaje = LimpiarMensaje(ex) });
            }
        }

        // Todo o nada: con un solo error (o en vista previa) no se guarda nada. Los clientes aún no
        // se agregaron al contexto, así que no hay nada que deshacer.
        if (respuesta.Errores.Count > 0 || !aplicarCambios)
        {
            respuesta.Aplicado = false;
            return respuesta;
        }

        await GuardarAsync(nuevos);
        respuesta.Aplicado = true;
        return respuesta;
    }

    private async Task GuardarAsync(List<Cliente> nuevos)
    {
        if (nuevos.Count == 0)
            return;

        _dbContext.Clientes.AddRange(nuevos);
        try
        {
            // Un solo SaveChanges = una sola transacción.
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();
            throw new InvalidOperationException(
                "No se guardó nada: alguna identificación o correo se registró mientras se procesaba el archivo. Vuelve a subirlo.");
        }
    }

    // ---------------------------------------------------------------------------------------
    // Armado de campos
    // ---------------------------------------------------------------------------------------

    // Persona jurídica: razón social. Natural: nombres y apellidos en el orden de las columnas.
    // Si falta la razón social de una jurídica, o el archivo mezcla ambas, se usa lo que haya.
    private static string ArmarNombre(string? tipoPersona, string? razonSocial, params string?[] partesNombre)
    {
        var esJuridica = string.Equals(tipoPersona, "JURIDICA", StringComparison.OrdinalIgnoreCase);
        var porPartes = string.Join(' ', partesNombre.Where(p => !string.IsNullOrWhiteSpace(p)));

        var elegido = esJuridica
            ? (razonSocial ?? porPartes)
            : (porPartes.Length > 0 ? porPartes : razonSocial ?? string.Empty);

        elegido = Regex.Replace(elegido, @"\s+", " ").Trim();
        if (elegido.Length == 0)
            throw new ErrorFilaImportacion("No tiene nombre ni razón social.");
        return elegido;
    }

    // El archivo trae teléfonos como "3 1 0 7 7 7 3 6 2 1": se dejan solo dígitos (y un "+" inicial).
    private static string NormalizarTelefono(string? crudo, int fila, ImportarClientesResponse respuesta)
    {
        if (crudo is null)
            return ClienteExcelFormato.TextoSinDato; // casi ningún tercero lo trae: no es un aviso

        var conMas = crudo.TrimStart().StartsWith('+');
        var digitos = new string(crudo.Where(char.IsDigit).ToArray());
        var normalizado = conMas ? "+" + digitos : digitos;

        if (digitos.Length < MinDigitosTelefono || normalizado.Length > MaxTelefono)
        {
            respuesta.Avisos.Add(Aviso(fila, $"El teléfono '{crudo}' no es válido: se importó sin teléfono."));
            return ClienteExcelFormato.TextoSinDato;
        }

        return normalizado;
    }

    // Un correo mal escrito o repetido no debe frenar al cliente: se importa sin correo y se avisa.
    private static string? ResolverEmail(string? crudo, int fila, HashSet<string> emailsEnUso, ImportarClientesResponse respuesta)
    {
        if (crudo is null)
            return null;

        var email = crudo.Trim().ToLowerInvariant();
        if (email.Length > MaxEmail || !FormatoEmail.IsMatch(email))
        {
            respuesta.Avisos.Add(Aviso(fila, $"El correo '{crudo}' no es válido: se importó sin correo."));
            return null;
        }

        if (!emailsEnUso.Add(email))
        {
            respuesta.Avisos.Add(Aviso(fila, $"El correo '{email}' ya lo usa otro cliente o empleado: se importó sin correo."));
            return null;
        }

        return email;
    }

    // Tipo de identificación del archivo -> tipo de documento fiscal del sistema.
    private static string? MapearTipoDocumento(string? tipo) => tipo?.Trim().ToUpperInvariant() switch
    {
        null or "" => null,
        "CC" => "CC",
        "NIT" => "NIT",
        "CE" => "CE",
        "PASAPORTE" or "PA" or "PP" => "Pasaporte",
        _ => "Otro",
    };

    private static string? LimpiarIdentificacion(string? crudo) =>
        crudo is null ? null : Regex.Replace(crudo, @"\s+", string.Empty);

    // Contraseña aleatoria que nadie conoce: el cliente la reemplaza con "Olvidé mi contraseña" o
    // con la que le asigne un administrador.
    private string GenerarHashAleatorio() =>
        _passwordHasher.HashPassword(null!, Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)));

    // ---------------------------------------------------------------------------------------
    // Lectura de celdas
    // ---------------------------------------------------------------------------------------

    private static ErrorImportacion Aviso(int fila, string mensaje) => new() { Fila = fila, Mensaje = mensaje };

    private static string? LeerTexto(IXLWorksheet hoja, Dictionary<string, int> columnas, int fila, string columna)
    {
        if (!columnas.TryGetValue(columna, out var numeroColumna))
            return null;

        var texto = hoja.Cell(fila, numeroColumna).GetString().Trim();
        return texto.Length == 0 ? null : texto;
    }

    private static bool FilaVacia(IXLWorksheet hoja, Dictionary<string, int> columnas, int fila) =>
        columnas.Values.All(columna => string.IsNullOrWhiteSpace(hoja.Cell(fila, columna).GetString()));

    // ArgumentException.Message termina en " (Parameter 'x')", que no le sirve al usuario.
    private static string LimpiarMensaje(Exception ex)
    {
        var mensaje = ex.Message;
        var indice = mensaje.IndexOf(" (Parameter '", StringComparison.Ordinal);
        return indice > 0 ? mensaje[..indice] : mensaje;
    }
}
