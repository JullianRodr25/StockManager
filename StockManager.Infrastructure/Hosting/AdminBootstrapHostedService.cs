using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text;
using StockManager.Infrastructure.Data;
using StockManager.Domain.Entities;

namespace StockManager.Infrastructure.Hosting;

/// <summary>
/// HostedService que se ejecuta una única vez al iniciar la aplicación.
/// Si la tabla Empleados está vacía, crea un Admin inicial con una contraseña aleatoria segura.
/// Esto resuelve el problema del "huevo y la gallina" para el registro de nuevos empleados.
/// </summary>
public class AdminBootstrapHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdminBootstrapHostedService> _logger;

    public AdminBootstrapHostedService(IServiceProvider serviceProvider, ILogger<AdminBootstrapHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                // Verificar si ya existen empleados
                var empleadosCount = await dbContext.Empleados.CountAsync(cancellationToken);

                if (empleadosCount == 0)
                {
                    _logger.LogInformation("Tabla Empleados vacía. Creando Admin inicial...");

                    // Generar contraseña aleatoria segura
                    var passwordGenerada = GenerarContraseñaSegura();

                    // Crear el Admin inicial con la contraseña generada
                    var passwordHasher = new PasswordHasher<Empleado>();
                    var passwordHash = passwordHasher.HashPassword(null!, passwordGenerada);

                    var adminInicial = Empleado.Crear(
                        numeroIdentificacion: "ADMIN001",
                        nombre: "Administrador",
                        email: "admin@stockmanager.local",
                        passwordHash: passwordHash,
                        rol: "Admin");

                    dbContext.Empleados.Add(adminInicial);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Admin inicial creado exitosamente.");
                    _logger.LogWarning("╔════════════════════════════════════════════════════════════════╗");
                    _logger.LogWarning("║              🔐 CREDENCIALES DEL ADMIN INICIAL 🔐               ║");
                    _logger.LogWarning("╠════════════════════════════════════════════════════════════════╣");
                    _logger.LogWarning($"║ Email: admin@stockmanager.local");
                    _logger.LogWarning($"║ NumeroIdentificacion: ADMIN001");
                    _logger.LogWarning($"║ Password: {passwordGenerada}");
                    _logger.LogWarning("╠════════════════════════════════════════════════════════════════╣");
                    _logger.LogWarning("║ ⚠️  IMPORTANTE:                                                  ║");
                    _logger.LogWarning("║ 1. Guarde estas credenciales en un lugar seguro               ║");
                    _logger.LogWarning("║ 2. Cambie la contraseña después del primer login              ║");
                    _logger.LogWarning("║ 3. No comparta estas credenciales                             ║");
                    _logger.LogWarning("╚════════════════════════════════════════════════════════════════╝");
                }
                else
                {
                    _logger.LogInformation("Empleados ya existen. Bootstrap no necesario.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante bootstrap del Admin inicial");
                throw;
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Genera una contraseña aleatoria segura con mínimo 16 caracteres.
    /// Incluye mayúsculas, minúsculas, números y símbolos.
    /// </summary>
    private static string GenerarContraseñaSegura()
    {
        const string mayusculas = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string minusculas = "abcdefghijklmnopqrstuvwxyz";
        const string numeros = "0123456789";
        const string simbolos = "!@#$%^&*-_=+";
        const int longitudMinima = 16;

        var todosLosCaracteres = mayusculas + minusculas + numeros + simbolos;
        var random = new Random();
        var passwordBuilder = new StringBuilder();

        // Garantizar al menos uno de cada tipo
        passwordBuilder.Append(mayusculas[random.Next(mayusculas.Length)]);
        passwordBuilder.Append(minusculas[random.Next(minusculas.Length)]);
        passwordBuilder.Append(numeros[random.Next(numeros.Length)]);
        passwordBuilder.Append(simbolos[random.Next(simbolos.Length)]);

        // Completar hasta la longitud mínima
        using (var rng = new RNGCryptoServiceProvider())
        {
            byte[] buffer = new byte[1];
            while (passwordBuilder.Length < longitudMinima)
            {
                rng.GetBytes(buffer);
                int index = buffer[0] % todosLosCaracteres.Length;
                passwordBuilder.Append(todosLosCaracteres[index]);
            }
        }

        // Mezclar la contraseña final
        var passwordArray = passwordBuilder.ToString().ToCharArray();
        using (var rng = new RNGCryptoServiceProvider())
        {
            byte[] buffer = new byte[1];
            for (int i = passwordArray.Length - 1; i > 0; i--)
            {
                rng.GetBytes(buffer);
                int randomIndex = buffer[0] % (i + 1);
                (passwordArray[i], passwordArray[randomIndex]) = (passwordArray[randomIndex], passwordArray[i]);
            }
        }

        return new string(passwordArray);
    }
}

