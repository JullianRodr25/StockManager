# StockManager API

Sistema de gestión de inventario para ferretera, construido con **.NET 10**, **Entity Framework Core**, **SQL Server** y **JWT Authentication**.

## 🎯 Características

- ✅ Autenticación JWT sin ASP.NET Core Identity completo
- ✅ Registro de usuarios (Empleados y Clientes)
- ✅ Autorización basada en roles (Admin, Empleado)
- ✅ Bootstrap automático del primer Admin
- ✅ Swagger UI clásico (Swashbuckle.AspNetCore)
- ✅ Base de datos con EF Core y SQL Server LocalDB
- ✅ Arquitectura en capas (Domain, Application, Infrastructure, API)
- ✅ Mitigación de timing attacks en login

## 📋 Requisitos

- **.NET 10 SDK** o superior
- **SQL Server** o **LocalDB**
- **Visual Studio 2022+** o cualquier editor compatible

## 🚀 Inicio Rápido

### 1. Clonar el repositorio

```bash
git clone <tu-repo-url>
cd StockManager
```

### 2. Restaurar dependencias

```bash
dotnet restore
```

### 3. Configurar base de datos

Actualiza `appsettings.json` con tu cadena de conexión:

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=StockManagerDb;Trusted_Connection=true;"
  },
  "JwtSettings": {
	"SecretKey": "tu-clave-secreta-muy-larga-minimo-32-caracteres!",
	"Issuer": "StockManagerAPI",
	"Audience": "StockManagerClients",
	"ExpirationMinutes": 60
  }
}
```

### 4. Ejecutar migraciones

```bash
cd StockManager.Infrastructure
dotnet ef database update
cd ..
```

### 5. Ejecutar la aplicación

```bash
dotnet run --project StockManager.Api
```

La API estará disponible en: `https://localhost:7xxx`

## 📚 Endpoints Principales

### Autenticación

- `POST /api/auth/login/empleado` - Login de empleado
- `POST /api/auth/login/cliente` - Login de cliente
- `POST /api/auth/registrar/empleado` - Registrar empleado (requiere [Authorize(Roles="Admin")])
- `POST /api/auth/registrar/cliente` - Registrar cliente (público)

## 🔐 Testing con JWT

Ver `TESTING_WITH_CURL.md` para ejemplos de cURL.

### Ejemplo rápido con PowerShell:

```powershell
# 1. Login
$response = Invoke-WebRequest -Uri "https://localhost:7xxx/api/auth/login/empleado" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"identificador":"admin001","password":"AdminPassword123!"}' `
  -SkipCertificateCheck

$token = ($response.Content | ConvertFrom-Json).token

# 2. Usar token en endpoint protegido
$headers = @{ "Authorization" = "Bearer $token" }
Invoke-WebRequest -Uri "https://localhost:7xxx/api/auth/registrar/empleado" `
  -Method Post `
  -Headers $headers `
  -ContentType "application/json" `
  -Body '{"numeroIdentificacion":"emp002","nombre":"Juan","email":"juan@test.com","password":"Pass123!","rol":"Empleado"}' `
  -SkipCertificateCheck
```

## 📁 Estructura del Proyecto

```
StockManager/
├── StockManager.Domain/              # Entidades, excepciones, reglas de negocio
├── StockManager.Application/         # DTOs, contratos de servicios
├── StockManager.Infrastructure/      # EF Core, implementación de servicios
├── StockManager.Api/                 # Controllers, Program.cs, Swagger
├── StockManager.sln                  # Archivo de solución
└── TESTING_WITH_CURL.md              # Documentación de testing
```

## ⚙️ Configuración JWT

El servicio de tokens genera JWTs con:
- **Algoritmo**: HS256
- **Expiración**: Configurable (default 60 minutos)
- **Claims**: Identificador, Rol, Tipo de usuario

## 🐛 Conocidos

- El botón "Authorize" en Swagger UI podría no aparecer visualmente en .NET 10 debido a conflictos de namespaces (`Microsoft.OpenApi.Models`). **Solución**: Usar cURL o agregar manualmente el header `Authorization: Bearer {token}` en "Try it out".

## 📝 Notas Adicionales

- **AdminBootstrapHostedService**: Crea automáticamente un Admin la primera vez que se ejecuta (si la tabla está vacía). La contraseña aleatoria segura se imprime en los logs de consola.
- **Timing Attack Mitigation**: El login usa hashes dummy para evitar ataques de timing.
- **Localización**: Algunos mensajes están en español (ajustable en `appsettings.json`).

## 🤝 Contribuciones

Las contribuciones son bienvenidas. Por favor:
1. Haz un fork
2. Crea una rama (`git checkout -b feature/MiFeature`)
3. Commit (`git commit -am 'Agrega MiFeature'`)
4. Push (`git push origin feature/MiFeature`)
5. Abre un Pull Request

## 📜 Licencia

Este proyecto está bajo licencia MIT.

---

**Última actualización**: Enero 2025  
**Versión**: 0.1.0-alpha
