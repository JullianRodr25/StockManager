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
    }
}
