# Testing StockManager API with cURL

## Preparación

Reemplaza en todos los comandos:
- `https://localhost:7xxx` con la URL real de tu API (ej: `https://localhost:7180`)
- Los valores de `numeroIdentificacion` y `password` según tu setup

## 1. Login de Empleado (para obtener JWT)

```powershell
$response = Invoke-WebRequest -Uri "https://localhost:7xxx/api/auth/login/empleado" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"identificador":"admin001","password":"AdminPassword123!"}' `
  -SkipCertificateCheck

$token = ($response.Content | ConvertFrom-Json).token
Write-Host "Token obtenido: $token"
```

O con curl:

```bash
curl -X POST https://localhost:7xxx/api/auth/login/empleado \
  -H "Content-Type: application/json" \
  -d '{"identificador":"admin001","password":"AdminPassword123!"}' \
  -k -s | jq .
```

## 2. Guardar el token

Una vez obtenido, guárdalo en una variable:

```powershell
$token = "eyJhbGciam..."  # Tu token aquí
```

## 3. Usar el token para acceder a endpoints protegidos

### Ejemplo: Registrar un nuevo empleado (requiere [Authorize])

```powershell
$headers = @{
	"Authorization" = "Bearer $token"
	"Content-Type" = "application/json"
}

$body = @{
	numeroIdentificacion = "empl002"
	nombre = "Juan Pérez"
	email = "juan@example.com"
	password = "NewPass123!"
	rol = "Empleado"
} | ConvertTo-Json

Invoke-WebRequest -Uri "https://localhost:7xxx/api/auth/registrar/empleado" `
  -Method Post `
  -Headers $headers `
  -Body $body `
  -SkipCertificateCheck
```

Con curl:

```bash
curl -X POST https://localhost:7xxx/api/auth/registrar/empleado \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"numeroIdentificacion":"empl002","nombre":"Juan Pérez","email":"juan@example.com","password":"NewPass123!","rol":"Empleado"}' \
  -k -s | jq .
```

## 4. Registrar un cliente (public endpoint, sin [Authorize])

```bash
curl -X POST https://localhost:7xxx/api/auth/registrar/cliente \
  -H "Content-Type: application/json" \
  -d '{"numeroIdentificacion":"cli001","nombre":"Carlos García","email":"carlos@example.com","password":"Pass123!","telefono":"555-1234","direccion":"Calle Principal 123"}' \
  -k -s | jq .
```

## 5. Ver Swagger UI

Abre en tu navegador:
```
https://localhost:7xxx/
```

*Nota: El botón "Authorize" podría no aparecer visualmente, pero puedes:*
1. Ejecutar `/api/auth/login/empleado` para obtener un token
2. Copiar el token
3. En cualquier endpoint protegido, hacer clic en "Try it out" → "Authentication" (si aparece)
4. O agregarlo manualmente en el header: `Authorization: Bearer {token}`

## Troubleshooting

- Si obtienes error **`Unauthorized`**: verifica que el token sea válido y no esté expirado
- Si obtienes error **`SSL/TLS certificate error`**: usa `-k` en curl o `-SkipCertificateCheck` en PowerShell
- Si obtienes error **`CORS`**: probablemente sea un endpoint público que necesita la configuración correcta

