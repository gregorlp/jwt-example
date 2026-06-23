# jwt-example

API mínima en ASP.NET Core (.NET 10) que demuestra autenticación con **JSON Web Tokens (JWT)**.

El flujo cubierto es:

1. El cliente obtiene un token mediante `POST /usuarios/login`.
2. El cliente envía ese token en el header `Authorization`.
3. Un endpoint protegido valida el JWT antes de responder.

## Demo para LinkedIn

Proyecto educativo pensado para compartir cómo funciona JWT en ASP.NET Core, no como sistema de login productivo.

**Flujo para capturas o video**

1. `POST /usuarios/login` → respuesta con `{ "token": "eyJ..." }`.
2. `GET /usuarios/test` con `Authorization: Bearer <token>` → **200** con los claims leídos del token.
3. (Opcional) Repetir el paso 2 sin token o con uno inválido → **401**.

**Archivos útiles para la demo**

- `jwt-example.http` — requests listos en el IDE.
- Scalar (Development) — documentación interactiva al levantar la API.

**Mensaje clave del post:** el middleware no solo comprueba que exista un header; valida firma, emisor, audiencia y expiración.

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- IDE compatible (Visual Studio, Rider, VS Code) u otro cliente HTTP (Postman, Scalar, etc.)

## Ejecución

Desde la carpeta del proyecto:

```bash
cd jwt-example
dotnet run
```

Perfiles disponibles en `Properties/launchSettings.json`:

| Perfil | URL |
|--------|-----|
| `http` | http://localhost:5001 |
| `https` | https://localhost:7294 / http://localhost:5136 |

En entorno **Development** también queda disponible la documentación interactiva de Scalar en la ruta configurada por el middleware OpenAPI.

## Configuración JWT

La configuración se define en `appsettings.json` y puede sobreescribirse en `appsettings.Development.json`:

```json
{
  "Jwt": {
    "SecretKey": "<clave-secreta-minimo-32-caracteres>",
    "Issuer": "mi-api",
    "Audience": "mi-frontend",
    "ExpirationMinutes": 60
  }
}
```

| Parámetro | Descripción |
|-----------|-------------|
| `SecretKey` | Clave simétrica usada para firmar y validar tokens. Debe tener longitud suficiente para HMAC-SHA256. |
| `Issuer` | Emisor esperado (`iss` en el token). |
| `Audience` | Audiencia esperada (`aud` en el token). |
| `ExpirationMinutes` | Duración del token en minutos. |

> **Importante:** no commitear claves reales de producción. Para desarrollo local, usar `appsettings.Development.json` o variables de entorno.

## Endpoints

### `POST /usuarios/login`

Genera un JWT y lo devuelve al cliente.

**Request**

```http
POST /usuarios/login
Content-Type: application/json

{
  "username": "demo",
  "password": "demo"
}
```

**Response `200 OK`**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Claims incluidos en el token**

| Claim | Valor de ejemplo |
|-------|------------------|
| `sub` | `123` |
| `email` | `usuario@test.com` |
| `role` | `Admin` |

**Notas**

- Las credenciales del body **no se validan** en esta demo; el endpoint siempre emite el mismo token de ejemplo.
- El issuer, audience y expiración del token se toman de la configuración JWT.

---

### `GET /usuarios/test`

Endpoint protegido. Requiere un JWT válido.

**Request**

```http
GET /usuarios/test
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response `200 OK`**

```json
{
  "message": "Token válido",
  "claims": [
    { "type": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "value": "123" },
    { "type": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", "value": "usuario@test.com" },
    { "type": "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "value": "Admin" }
  ]
}
```

**Response `401 Unauthorized`**

Se devuelve cuando:

- No se envía header `Authorization`.
- El header no usa el formato `Bearer <token>`.
- El token está mal formado, expirado, tiene firma inválida, o issuer/audience incorrectos.

## Validación del token

La validación la realiza el middleware **JWT Bearer** configurado en `Program.cs`. No basta con que exista un valor en el header: se comprueban todos estos aspectos:

| Validación | Configuración |
|------------|---------------|
| Firma | `ValidateIssuerSigningKey = true` con `SecretKey` |
| Emisor | `ValidateIssuer = true`, `ValidIssuer = "mi-api"` |
| Audiencia | `ValidateAudience = true`, `ValidAudience = "mi-frontend"` |
| Expiración | `ValidateLifetime = true` |

Pipeline de middleware relevante:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

El atributo `[Authorize]` en `/usuarios/test` exige autenticación exitosa. **No valida roles** por sí solo; cualquier token válido es suficiente.

Para exigir un rol concreto se podría usar, por ejemplo:

```csharp
[Authorize(Roles = "Admin")]
```

## Cómo probar correctamente

### Flujo recomendado

1. Ejecutar la API con `dotnet run`.
2. Llamar a `POST /usuarios/login` y copiar el valor completo de `token`.
3. Llamar a `GET /usuarios/test` con el header:

   ```http
   Authorization: Bearer <token_completo>
   ```

### Errores frecuentes

| Error | Causa |
|-------|-------|
| `401 Unauthorized` | Header incorrecto o token inválido |
| Usar header `Bearer` como nombre | Incorrecto. El nombre del header debe ser `Authorization` |
| Enviar la `SecretKey` en lugar del JWT | Incorrecto. Debe enviarse el token devuelto por `/login` |
| Token truncado | Un JWT tiene tres partes separadas por `.` |

### Ejemplo con curl

```bash
# 1. Login
curl -k -X POST https://localhost:7294/usuarios/login \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"demo\",\"password\":\"demo\"}"

# 2. Endpoint protegido (reemplazar TOKEN)
curl -k https://localhost:7294/usuarios/test \
  -H "Authorization: Bearer TOKEN"
```

### Ejemplo en `jwt-example.http`

```http
@host = https://localhost:7294

### Login
POST {{host}}/usuarios/login
Content-Type: application/json

{
  "username": "demo",
  "password": "demo"
}

### Endpoint protegido
@token = pegar_aqui_el_token_del_login

GET {{host}}/usuarios/test
Authorization: Bearer {{token}}
```

## Estructura del proyecto

```
jwt-example/
├── Controllers/
│   └── UsuariosController.cs   # Login y endpoint protegido
├── JWT/
│   └── JwtOptions.cs           # Modelo de configuración JWT
├── Model/
│   ├── LoginViewModel.cs       # Request de login
│   └── LoginResponse.cs        # Response con token
├── Program.cs                  # Configuración de auth y pipeline
├── appsettings.json
└── appsettings.Development.json
```

## Dependencias principales

- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Microsoft.IdentityModel.JsonWebTokens`
- `Microsoft.AspNetCore.OpenApi`
- `Scalar.AspNetCore` (documentación en Development)

## Limitaciones de esta demo

Este proyecto está pensado para **demostrar el mecanismo JWT**, no como base de producción:

- No hay validación real de usuario/contraseña.
- Los claims del token están hardcodeados.
- No hay refresh tokens ni revocación de tokens.
- `[Authorize]` valida identidad, no permisos por rol.

## Licencia

Proyecto de ejemplo con fines educativos.
