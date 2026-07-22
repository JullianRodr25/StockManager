using System.ComponentModel.DataAnnotations;

namespace StockManager.Application.DTOs;

// Login DTOs
public record AuthRequest(string Identificador, string Password);
public record AuthResponse(string Token);

// Registro DTOs
public record RegistrarEmpleadoRequest(
    [Required(ErrorMessage = "El número de identificación es requerido")]
    string NumeroIdentificacion,

    [Required(ErrorMessage = "El nombre es requerido")]
    string Nombre,

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
    string Email,

    [Required(ErrorMessage = "La contraseña es requerida")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    string Password,

    [Required(ErrorMessage = "El rol es requerido")]
    string Rol);

public record RegistrarClienteRequest(
    [Required(ErrorMessage = "El número de identificación es requerido")]
    string NumeroIdentificacion,

    [Required(ErrorMessage = "El nombre es requerido")]
    string Nombre,

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
    string Email,

    [Required(ErrorMessage = "La contraseña es requerida")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    string Password,

    [Required(ErrorMessage = "El teléfono es requerido")]
    string Telefono,

    [Required(ErrorMessage = "La dirección es requerida")]
    string Direccion);

public record RegistrarResponse(int Id, string Message);
