using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

/// <summary>
/// Estados de Pedido que se consideran "activos" para efectos de bloquear la desactivación
/// de un Cliente: mientras exista uno de estos, todavía hay una entrega en curso que
/// depende de que el cliente siga activo.
/// </summary>
public class ClienteService : IClienteService
{
    private static readonly string[] EstadosPedidoActivos = { "Pendiente", "Confirmado", "EnPreparacion", "EnCamino" };

    private readonly AppDbContext _dbContext;
    private readonly PasswordHasher<Cliente> _passwordHasher;

    public ClienteService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
        _passwordHasher = new PasswordHasher<Cliente>();
    }

    public async Task<List<ClienteResponse>> BuscarClientesAsync(string? busqueda, bool? activo = null)
    {
        var query = _dbContext.Clientes.AsNoTracking().AsQueryable();

        if (activo.HasValue)
            query = query.Where(c => c.Activo == activo.Value);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim().ToUpper();
            query = query.Where(c =>
                c.Nombre.ToUpper().Contains(termino) ||
                c.NumeroIdentificacion.ToUpper().Contains(termino));
        }

        return await query
            .OrderBy(c => c.Nombre)
            .Take(50)
            .Select(c => new ClienteResponse(
                c.Id,
                c.NumeroIdentificacion,
                c.Nombre,
                c.Email,
                c.Telefono,
                c.Direccion,
                c.Activo,
                c.OrigenRegistro,
                c.TipoDocumentoFiscal,
                c.NumeroDocumentoFiscal,
                c.RazonSocialFiscal,
                c.DireccionFiscal,
                c.EmailFacturacion,
                c.TieneDatosFacturacionElectronicaCompletos,
                c.FotoUrl))
            .ToListAsync();
    }

    public async Task<ClienteResponse?> ObtenerClientePorIdAsync(int id)
    {
        return await _dbContext.Clientes
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ClienteResponse(
                c.Id,
                c.NumeroIdentificacion,
                c.Nombre,
                c.Email,
                c.Telefono,
                c.Direccion,
                c.Activo,
                c.OrigenRegistro,
                c.TipoDocumentoFiscal,
                c.NumeroDocumentoFiscal,
                c.RazonSocialFiscal,
                c.DireccionFiscal,
                c.EmailFacturacion,
                c.TieneDatosFacturacionElectronicaCompletos,
                c.FotoUrl))
            .FirstOrDefaultAsync();
    }

    public async Task<ClienteCreadoResponse> CrearClienteAsync(CrearClienteRequest request)
    {
        var numeroNormalizado = request.NumeroIdentificacion.Trim();
        var existeNumero = await _dbContext.Clientes.AnyAsync(c => c.NumeroIdentificacion == numeroNormalizado);
        if (existeNumero)
            throw new UsuarioDuplicadoPorIdentificacionException(numeroNormalizado);

        var emailNormalizado = request.Email.Trim().ToLower();
        var existeEmailCliente = await _dbContext.Clientes.AnyAsync(c => c.Email == emailNormalizado);
        var existeEmailEmpleado = await _dbContext.Empleados.AnyAsync(e => e.Email == emailNormalizado);
        if (existeEmailCliente || existeEmailEmpleado)
            throw new UsuarioDuplicadoPorEmailException(emailNormalizado);

        // Si el empleado no especifica una contraseña (caso típico: cliente de mostrador que
        // no va a usar la PWA de inmediato), se genera una temporal y se devuelve una sola vez.
        var passwordGenerada = string.IsNullOrWhiteSpace(request.Password) ? GenerarPasswordTemporal() : null;
        var passwordAUsar = passwordGenerada ?? request.Password!;

        var passwordHash = _passwordHasher.HashPassword(null!, passwordAUsar);

        // Único endpoint de creación "manual" (panel/caja); el autoregistro público de la PWA
        // pasa por AuthService.RegistrarClienteAsync, que usa origenRegistro "Pwa".
        var cliente = Cliente.Crear(
            numeroIdentificacion: numeroNormalizado,
            nombre: request.Nombre,
            email: emailNormalizado,
            passwordHash: passwordHash,
            telefono: request.Telefono,
            direccion: request.Direccion,
            origenRegistro: "Caja");

        if (request.TipoDocumentoFiscal is not null || request.NumeroDocumentoFiscal is not null ||
            request.RazonSocialFiscal is not null || request.DireccionFiscal is not null ||
            request.EmailFacturacion is not null)
        {
            cliente.ActualizarDatosFacturacionElectronica(
                request.TipoDocumentoFiscal,
                request.NumeroDocumentoFiscal,
                request.RazonSocialFiscal,
                request.DireccionFiscal,
                request.EmailFacturacion);
        }

        _dbContext.Clientes.Add(cliente);
        await _dbContext.SaveChangesAsync();

        return new ClienteCreadoResponse(MapearResponse(cliente), passwordGenerada);
    }

    public async Task<ClienteResponse> ActualizarDatosFacturacionElectronicaAsync(int id, ActualizarDatosFacturacionRequest request)
    {
        var cliente = await _dbContext.Clientes.FirstOrDefaultAsync(c => c.Id == id);
        if (cliente == null)
            throw new ClienteNoEncontradoException(id);

        cliente.ActualizarDatosFacturacionElectronica(
            request.TipoDocumentoFiscal,
            request.NumeroDocumentoFiscal,
            request.RazonSocialFiscal,
            request.DireccionFiscal,
            request.EmailFacturacion);

        await _dbContext.SaveChangesAsync();

        return MapearResponse(cliente);
    }

    public async Task<ClienteResponse> ActualizarClienteAsync(int id, ActualizarClienteRequest request)
    {
        var cliente = await _dbContext.Clientes.FirstOrDefaultAsync(c => c.Id == id);
        if (cliente == null)
            throw new ClienteNoEncontradoException(id);

        var emailNormalizado = request.Email.Trim().ToLower();
        if (emailNormalizado != cliente.Email)
        {
            var existeEmailCliente = await _dbContext.Clientes.AnyAsync(c => c.Id != id && c.Email == emailNormalizado);
            var existeEmailEmpleado = await _dbContext.Empleados.AnyAsync(e => e.Email == emailNormalizado);
            if (existeEmailCliente || existeEmailEmpleado)
                throw new UsuarioDuplicadoPorEmailException(emailNormalizado);
        }

        cliente.ActualizarInformacion(request.Nombre, emailNormalizado, request.Telefono, request.Direccion);
        await _dbContext.SaveChangesAsync();

        return MapearResponse(cliente);
    }

    public async Task<ClienteResponse> DesactivarClienteAsync(int id)
    {
        var cliente = await _dbContext.Clientes.FirstOrDefaultAsync(c => c.Id == id);
        if (cliente == null)
            throw new ClienteNoEncontradoException(id);

        var tienePedidosActivos = await _dbContext.Pedidos
            .AnyAsync(p => p.ClienteId == id && EstadosPedidoActivos.Contains(p.Estado));
        if (tienePedidosActivos)
            throw new ClienteConPedidosActivosException(id);

        cliente.Desactivar();
        await _dbContext.SaveChangesAsync();

        return MapearResponse(cliente);
    }

    public async Task<ClienteResponse> ActivarClienteAsync(int id)
    {
        var cliente = await _dbContext.Clientes.FirstOrDefaultAsync(c => c.Id == id);
        if (cliente == null)
            throw new ClienteNoEncontradoException(id);

        cliente.Activar();
        await _dbContext.SaveChangesAsync();

        return MapearResponse(cliente);
    }

    public async Task CambiarPasswordPropioAsync(int clienteId, CambiarPasswordPropioRequest request)
    {
        var cliente = await _dbContext.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId);
        if (cliente == null)
            throw new ClienteNoEncontradoException(clienteId);

        var resultado = _passwordHasher.VerifyHashedPassword(cliente, cliente.PasswordHash, request.PasswordActual);
        if (resultado == PasswordVerificationResult.Failed)
            throw new ContrasenaActualIncorrectaException();

        var nuevoHash = _passwordHasher.HashPassword(cliente, request.PasswordNueva);
        cliente.ActualizarPasswordHash(nuevoHash);

        await _dbContext.SaveChangesAsync();
    }

    private static ClienteResponse MapearResponse(Cliente cliente) => new(
        cliente.Id,
        cliente.NumeroIdentificacion,
        cliente.Nombre,
        cliente.Email,
        cliente.Telefono,
        cliente.Direccion,
        cliente.Activo,
        cliente.OrigenRegistro,
        cliente.TipoDocumentoFiscal,
        cliente.NumeroDocumentoFiscal,
        cliente.RazonSocialFiscal,
        cliente.DireccionFiscal,
        cliente.EmailFacturacion,
        cliente.TieneDatosFacturacionElectronicaCompletos,
        cliente.FotoUrl);

    /// <summary>
    /// Genera una contraseña temporal aleatoria de 12 caracteres (mayúsculas, minúsculas,
    /// números y símbolos) usando un RNG criptográfico. Mismo enfoque que
    /// AdminBootstrapHostedService para el admin inicial, en una versión más corta ya que
    /// esta sí se entrega directamente al cliente para su primer login.
    /// </summary>
    private static string GenerarPasswordTemporal()
    {
        const string caracteres = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        const int longitud = 12;

        var resultado = new StringBuilder(longitud);
        var buffer = new byte[1];
        using var rng = RandomNumberGenerator.Create();
        for (var i = 0; i < longitud; i++)
        {
            rng.GetBytes(buffer);
            resultado.Append(caracteres[buffer[0] % caracteres.Length]);
        }

        return resultado.ToString();
    }
}
