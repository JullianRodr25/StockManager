namespace StockManager.Application.DTOs;

public record CrearProveedorRequest(
    string Nombre,
    string? NumeroIdentificacion,
    string? Telefono,
    string? Email,
    string? Direccion,
    string? NumeroWhatsApp
);

public record ActualizarProveedorRequest(
    string Nombre,
    string? NumeroIdentificacion,
    string? Telefono,
    string? Email,
    string? Direccion,
    string? NumeroWhatsApp
);

public record ProveedorResponse(
    int Id,
    string Nombre,
    string? NumeroIdentificacion,
    string? Telefono,
    string? Email,
    string? Direccion,
    bool Activo,
    string? NumeroWhatsApp
);
