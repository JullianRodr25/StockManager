using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockManager.Application.Services;
using StockManager.Application.DTOs;
using StockManager.Infrastructure.Data;
using StockManager.Infrastructure.Notificaciones;
using StockManager.Domain.Constants;
using StockManager.Domain.Entities;
using StockManager.Domain.Exceptions;

namespace StockManager.Infrastructure.Services
{
    /// <summary>
    /// Servicio de autenticación para Empleados y Clientes.
    /// Verifica password contra PasswordHash usando PasswordHasher<T> y genera JWT vía ITokenService.
    /// Mitiga timing attacks ejecutando VerifyHashedPassword contra un hash dummy cuando el usuario no existe.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly ITokenService _tokenService;
        private readonly IPasswordResetTokenService _passwordResetTokenService;
        private readonly IEmailSender _emailSender;
        private readonly EmailOptions _emailOpciones;
        private readonly ILogger<AuthService> _logger;
        private readonly PasswordHasher<Empleado> _passwordHasherEmpleado;
        private readonly PasswordHasher<Cliente> _passwordHasherCliente;

        // Dummy hashes (generados una vez) para igualar el tiempo de respuesta cuando el usuario no existe
        private static readonly string _dummyEmpleadoHash = new PasswordHasher<Empleado>().HashPassword((Empleado?)null!, "DummyPassword123!");
        private static readonly string _dummyClienteHash = new PasswordHasher<Cliente>().HashPassword((Cliente?)null!, "DummyPassword123!");

        public AuthService(
            AppDbContext db,
            ITokenService tokenService,
            IPasswordResetTokenService passwordResetTokenService,
            IEmailSender emailSender,
            IOptions<EmailOptions> emailOpciones,
            ILogger<AuthService> logger)
        {
            _db = db;
            _tokenService = tokenService;
            _passwordResetTokenService = passwordResetTokenService;
            _emailSender = emailSender;
            _emailOpciones = emailOpciones.Value;
            _logger = logger;
            _passwordHasherEmpleado = new PasswordHasher<Empleado>();
            _passwordHasherCliente = new PasswordHasher<Cliente>();
        }

        // ===== LOGIN METHODS =====

        public async Task<string?> LoginEmpleadoAsync(string identificador, string password)
        {
            if (string.IsNullOrWhiteSpace(identificador) || string.IsNullOrWhiteSpace(password))
                return null;

            Empleado? empleado;
            if (identificador.Contains("@"))
            {
                var email = identificador.Trim().ToLower();
                empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.Email == email);
            }
            else
            {
                var numero = identificador.Trim();
                empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.NumeroIdentificacion == numero);
            }

            // Seleccionar el hash a verificar: real si existe, dummy si no
            var hashToVerify = empleado?.PasswordHash ?? _dummyEmpleadoHash;

            var result = _passwordHasherEmpleado.VerifyHashedPassword(empleado, hashToVerify, password);

            // Solo generar token si el usuario existe y la verificación fue exitosa
            if (empleado != null && (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded))
            {
                return _tokenService.GenerarTokenEmpleado(empleado.Id, empleado.NumeroIdentificacion, empleado.Nombre, empleado.Rol);
            }

            return null;
        }

        public async Task<string?> LoginClienteAsync(string identificador, string password)
        {
            if (string.IsNullOrWhiteSpace(identificador) || string.IsNullOrWhiteSpace(password))
                return null;

            Cliente? cliente;
            if (identificador.Contains("@"))
            {
                var email = identificador.Trim().ToLower();
                cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Email == email);
            }
            else
            {
                var numero = identificador.Trim();
                cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.NumeroIdentificacion == numero);
            }

            var hashToVerify = cliente?.PasswordHash ?? _dummyClienteHash;

            var result = _passwordHasherCliente.VerifyHashedPassword(cliente, hashToVerify, password);

            if (cliente != null && (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded))
            {
                return _tokenService.GenerarTokenCliente(cliente.Id, cliente.NumeroIdentificacion, cliente.Nombre);
            }

            return null;
        }

        // ===== REGISTRO METHODS =====

        public async Task<int> RegistrarEmpleadoAsync(RegistrarEmpleadoRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Validar que el rol sea válido
            if (!Roles.EsRolDeEmpleado(request.Rol))
                throw new ArgumentException("El rol debe ser 'Admin', 'Empleado' o 'Inventario'", nameof(request.Rol));

            // Validar que no exista otro empleado con el mismo NumeroIdentificacion
            var numeroNormalizado = request.NumeroIdentificacion.Trim();
            var existeNumero = await _db.Empleados
                .AnyAsync(e => e.NumeroIdentificacion == numeroNormalizado);
            if (existeNumero)
                throw new UsuarioDuplicadoPorIdentificacionException(numeroNormalizado);

            // Validar que no exista otro empleado con el mismo Email
            var emailNormalizado = request.Email.Trim().ToLower();
            var existeEmail = await _db.Empleados
                .AnyAsync(e => e.Email == emailNormalizado);
            if (existeEmail)
                throw new UsuarioDuplicadoPorEmailException(emailNormalizado);

            // Hashear el password
            var passwordHash = _passwordHasherEmpleado.HashPassword(null!, request.Password);

            // Usar el factory method Empleado.Crear
            var empleado = Empleado.Crear(
                numeroIdentificacion: numeroNormalizado,
                nombre: request.Nombre,
                email: emailNormalizado,
                passwordHash: passwordHash,
                rol: request.Rol);

            // Agregar a la base de datos
            _db.Empleados.Add(empleado);
            await _db.SaveChangesAsync();

            return empleado.Id;
        }

        public async Task<int> RegistrarClienteAsync(RegistrarClienteRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Validar que no exista otro cliente con el mismo NumeroIdentificacion
            var numeroNormalizado = request.NumeroIdentificacion.Trim();
            var existeNumero = await _db.Clientes
                .AnyAsync(c => c.NumeroIdentificacion == numeroNormalizado);
            if (existeNumero)
                throw new UsuarioDuplicadoPorIdentificacionException(numeroNormalizado);

            // Validar que no exista otro cliente con el mismo Email
            var emailNormalizado = request.Email.Trim().ToLower();
            var existeEmail = await _db.Clientes
                .AnyAsync(c => c.Email == emailNormalizado);
            if (existeEmail)
                throw new UsuarioDuplicadoPorEmailException(emailNormalizado);

            // También validar que no exista un empleado con el mismo email (opcional pero seguro)
            var existeEmailEmpleado = await _db.Empleados
                .AnyAsync(e => e.Email == emailNormalizado);
            if (existeEmailEmpleado)
                throw new UsuarioDuplicadoPorEmailException(emailNormalizado);

            // Hashear el password
            var passwordHash = _passwordHasherCliente.HashPassword(null!, request.Password);

            // Usar el factory method Cliente.Crear. Este es el único punto de autoregistro
            // público (PWA), así que el origen siempre es "Pwa".
            var cliente = Cliente.Crear(
                numeroIdentificacion: numeroNormalizado,
                nombre: request.Nombre,
                email: emailNormalizado,
                passwordHash: passwordHash,
                telefono: request.Telefono,
                direccion: request.Direccion,
                origenRegistro: "Pwa");

            // Agregar a la base de datos
            _db.Clientes.Add(cliente);
            await _db.SaveChangesAsync();

            return cliente.Id;
        }

        // ===== RECUPERACIÓN DE CONTRASEÑA =====

        public async Task SolicitarRecuperacionAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return;

            var emailNormalizado = email.Trim().ToLower();

            // El email es globalmente único entre Empleados y Clientes (ver
            // RegistrarClienteAsync arriba), así que basta con buscar en ambas tablas para
            // saber a quién pertenece, sin que el llamador tenga que indicar el tipo de usuario.
            var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.Email == emailNormalizado);
            var cliente = empleado == null
                ? await _db.Clientes.FirstOrDefaultAsync(c => c.Email == emailNormalizado)
                : null;

            // Si el email no está registrado, no se envía nada — pero tampoco se informa al
            // llamador (mismo principio anti-enumeración que el login: la respuesta pública es
            // idéntica exista o no la cuenta).
            if (empleado == null && cliente == null)
                return;

            var vigencia = TimeSpan.FromMinutes(_emailOpciones.RecuperacionVigenciaMinutos);
            string tipoUsuario;
            int usuarioId;
            string nombre;
            string destinatario;
            string urlBase;

            if (empleado != null)
            {
                tipoUsuario = "Empleado";
                usuarioId = empleado.Id;
                nombre = empleado.Nombre;
                destinatario = empleado.Email;
                urlBase = _emailOpciones.FrontendBaseUrlPanel;
            }
            else
            {
                tipoUsuario = "Cliente";
                usuarioId = cliente!.Id;
                nombre = cliente.Nombre;
                destinatario = cliente.Email!; // se encontró buscando por ese correo
                urlBase = _emailOpciones.FrontendBaseUrlPwa;
            }

            var token = _passwordResetTokenService.GenerarToken(tipoUsuario, usuarioId, vigencia);
            var link = $"{urlBase.TrimEnd('/')}/restablecer-contrasena?token={Uri.EscapeDataString(token)}";

            var htmlContenido = $"""
                <p>Hola {System.Net.WebUtility.HtmlEncode(nombre)},</p>
                <p>Recibimos una solicitud para restablecer tu contraseña. Si fuiste tú, hacé clic en el siguiente enlace (válido por {_emailOpciones.RecuperacionVigenciaMinutos} minutos):</p>
                <p><a href="{link}">Restablecer mi contraseña</a></p>
                <p>Si no solicitaste esto, podés ignorar este correo — tu contraseña actual sigue siendo válida.</p>
                """;

            // Envío de "mejor esfuerzo": un fallo del proveedor de correo no debe filtrarse
            // como un error al usuario que pidió recuperar su contraseña (misma respuesta
            // genérica en ambos casos), solo queda registrado en el log.
            try
            {
                var resultado = await _emailSender.EnviarAsync(destinatario, nombre, "Restablecer tu contraseña", htmlContenido);
                if (!resultado.Exitoso)
                    _logger.LogWarning("No se pudo enviar el correo de recuperación a {Email}: {Error}", destinatario, resultado.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado enviando correo de recuperación a {Email}", destinatario);
            }
        }

        public async Task<bool> RestablecerContrasenaAsync(string token, string nuevaPassword)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(nuevaPassword))
                return false;

            var payload = _passwordResetTokenService.ValidarToken(token);
            if (payload == null)
                return false;

            if (payload.TipoUsuario == "Empleado")
            {
                var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.Id == payload.UsuarioId);
                if (empleado == null)
                    return false;

                var nuevoHash = _passwordHasherEmpleado.HashPassword(empleado, nuevaPassword);
                empleado.ActualizarPasswordHash(nuevoHash);
            }
            else
            {
                var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == payload.UsuarioId);
                if (cliente == null)
                    return false;

                var nuevoHash = _passwordHasherCliente.HashPassword(cliente, nuevaPassword);
                cliente.ActualizarPasswordHash(nuevoHash);
            }

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
