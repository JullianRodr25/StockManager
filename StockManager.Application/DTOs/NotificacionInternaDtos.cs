namespace StockManager.Application.DTOs;

public record NotificacionInternaResponse(
    int Id,
    string Tipo,
    string Titulo,
    string Mensaje,
    string EntidadTipo,
    int EntidadId,
    DateTime FechaCreacion,
    bool Leida
);
