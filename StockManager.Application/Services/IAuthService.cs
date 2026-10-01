using System.Threading.Tasks;
using StockManager.Application.DTOs;

namespace StockManager.Application.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// Intenta autenticar un empleado por identificador (NumeroIdentificacion o Email) y password.
        /// Retorna token JWT si tiene éxito, o null si las credenciales son inválidas.
        /// </summary>
        Task<string?> LoginEmpleadoAsync(string identificador, string password);

        /// <summary>
        /// Intenta autenticar un cliente por identificador (NumeroIdentificacion o Email) y password.
        /// Retorna token JWT si tiene éxito, o null si las credenciales son inválidas.
        /// </summary>
        Task<string?> LoginClienteAsync(string identificador, string password);

        /// <summary>
        /// Registra un nuevo empleado con los datos proporcionados.
        /// Valida que no exista otro empleado con el mismo NumeroIdentificacion o Email.
        /// Hashea el password y usa el factory method Empleado.Crear.
        /// Retorna el ID del empleado creado.
        /// Lanza excepciones de dominio si hay duplicación.
        /// </summary>
        Task<int> RegistrarEmpleadoAsync(RegistrarEmpleadoRequest request);

        /// <summary>
        /// Registra un nuevo cliente con los datos proporcionados.
        /// Valida que no exista otro cliente con el mismo NumeroIdentificacion o Email.
        /// Hashea el password y usa el factory method Cliente.Crear.
        /// Retorna el ID del cliente creado.
        /// Lanza excepciones de dominio si hay duplicación.
        /// </summary>
        Task<int> RegistrarClienteAsync(RegistrarClienteRequest request);

        /// <summary>
        /// Inicia el flujo de recuperación de contraseña: si el email pertenece a un Empleado
        /// o a un Cliente (son globalmente únicos entre ambas tablas), envía un correo con un
        /// link firmado y temporal para restablecer la contraseña. Si el email no está
        /// registrado, no hace nada — pero de cara al llamador el comportamiento es idéntico
        /// en ambos casos (no lanza excepción, no indica si el email existía), para no revelar
        /// qué correos están registrados en el sistema.
        /// </summary>
        Task SolicitarRecuperacionAsync(string email);

        /// <summary>
        /// Completa el flujo de recuperación de contraseña: valida el token firmado (emitido
        /// por SolicitarRecuperacionAsync) y, si es válido y no ha expirado, actualiza el hash
        /// de contraseña del Empleado o Cliente al que pertenece. Retorna false si el token es
        /// inválido, expiró, o el usuario ya no existe.
        /// </summary>
        Task<bool> RestablecerContrasenaAsync(string token, string nuevaPassword);
    }
}
