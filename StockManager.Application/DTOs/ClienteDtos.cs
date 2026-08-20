namespace StockManager.Application.DTOs;

public record ClienteResponse(
    int Id,
    string NumeroIdentificacion,
    string Nombre,
    string Email,
    string Telefono,
    string Direccion,
    bool Activo
);
