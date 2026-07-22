using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockManager.Application.Services;
using StockManager.Application.DTOs;
using StockManager.Infrastructure.Data;
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
        private readonly PasswordHasher<Empleado> _passwordHasherEmpleado;
        private readonly PasswordHasher<Cliente> _passwordHasherCliente;

        // Dummy hashes (generados una vez) para igualar el tiempo de respuesta cuando el usuario no existe
        private static readonly string _dummyEmpleadoHash = new PasswordHasher<Empleado>().HashPassword((Empleado?)null!, "DummyPassword123!");
        private static readonly string _dummyClienteHash = new PasswordHasher<Cliente>().HashPassword((Cliente?)null!, "DummyPassword123!");

        public AuthService(AppDbContext db, ITokenService tokenService)
        {
            _db = db;
            _tokenService = tokenService;
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
            if (request.Rol != "Admin" && request.Rol != "Empleado")
                throw new ArgumentException("El rol debe ser 'Admin' o 'Empleado'", nameof(request.Rol));

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

            // Usar el factory method Cliente.Crear
            var cliente = Cliente.Crear(
                numeroIdentificacion: numeroNormalizado,
                nombre: request.Nombre,
                email: emailNormalizado,
                passwordHash: passwordHash,
                telefono: request.Telefono,
                direccion: request.Direccion);

            // Agregar a la base de datos
            _db.Clientes.Add(cliente);
            await _db.SaveChangesAsync();

            return cliente.Id;
        }
    }
}
