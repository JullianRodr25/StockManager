namespace StockManager.Application.DTOs;

public record CrearProveedorRequest(
    string Nombre,
    string? NumeroIdentificacion,
    string? Telefono,
    string? Email,
    string? Direccion
);

public record ActualizarProveedorRequest(
    string Nombre,
    string? NumeroIdentificacion,
    string? Telefono,
    string? Email,
    string? Direccion
);

public record ProveedorResponse(
    int Id,
    string Nombre,
    string? NumeroIdentificacion,
    string? Telefono,
    string? Email,
    string? Direccion,
    bool Activo
);
