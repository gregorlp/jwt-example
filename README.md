# jwt-example

Minimal ASP.NET Core (.NET 10) API demonstrating **JSON Web Token (JWT)** authentication.

The flow covered:

1. The client obtains a token via `POST /usuarios/login`.
2. The client sends that token in the `Authorization` header.
3. A protected endpoint validates the JWT before responding.

## LinkedIn demo

Educational project to share how JWT works in ASP.NET Core — not a production login system.

**Flow for screenshots or a short video**

1. `POST /usuarios/login` → response with `{ "token": "eyJ..." }`.
2. `GET /usuarios/test` with `Authorization: Bearer <token>` → **200** with claims read from the token.
3. (Optional) Repeat step 2 without a token or with an invalid one → **401**.

**Useful files for the demo**

- `jwt-example.http` — ready-to-run requests in your IDE.
- Scalar (Development) — interactive API docs when the app is running.

**Key message for the post:** the middleware does not only check that a header exists; it validates signature, issuer, audience, and expiration.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A compatible IDE (Visual Studio, Rider, VS Code) or an HTTP client (Postman, Scalar, etc.)

## Running the project

1. Configure User Secrets locally (first time only):

   ```bash
   cd jwt-example

   # Only if the .csproj does not have UserSecretsId yet:
   dotnet user-secrets init

   dotnet user-secrets set "Jwt:SecretKey" "your-secret-key-at-least-32-characters-long"
   ```

   > This repository **already includes** `UserSecretsId` in the `.csproj`. When cloning, the `set` command is enough. Run `init` only when creating the project from scratch or if the `.csproj` is missing that property.

2. Start the API:

   ```bash
   dotnet run
   ```

Profiles in `Properties/launchSettings.json`:

| Profile | URL |
|---------|-----|
| `http` | http://localhost:5001 |
| `https` | https://localhost:7294 / http://localhost:5136 |

In **Development**, Scalar interactive documentation is also available via the OpenAPI middleware.

## JWT configuration

Settings live in `appsettings.json`. The repository **does not include a real secret**; it uses a placeholder:

```json
{
  "Jwt": {
    "SecretKey": "YOUR_SECRET_KEY_HERE_MIN_32_CHARS",
    "Issuer": "mi-api",
    "Audience": "mi-frontend",
    "ExpirationMinutes": 60
  }
}
```

| Parameter | Description |
|-----------|-------------|
| `SecretKey` | Symmetric key used to sign and validate tokens. Must be long enough for HMAC-SHA256. |
| `Issuer` | Expected token issuer (`iss` claim). |
| `Audience` | Expected token audience (`aud` claim). |
| `ExpirationMinutes` | Token lifetime in minutes. |

### Configure the secret locally (required before testing)

**User Secrets** stores the key outside the repository. Steps:

```bash
cd jwt-example

# 1) Enable User Secrets (once per project)
dotnet user-secrets init

# 2) Save the local secret
dotnet user-secrets set "Jwt:SecretKey" "your-secret-key-at-least-32-characters-long"
```

`dotnet user-secrets init` adds `UserSecretsId` to the `.csproj`. **This repo is already configured** — when cloning, skip straight to step 2.

Verify it exists in the `.csproj`:

```powershell
Select-String "UserSecretsId" jwt-example.csproj
```

Alternative using an environment variable:

```bash
# PowerShell
$env:Jwt__SecretKey = "your-secret-key-at-least-32-characters-long"

# bash
export Jwt__SecretKey="your-secret-key-at-least-32-characters-long"
```

In Development, **User Secrets** and environment variables override `appsettings.json`.

> **Important:** never commit real secrets to the repository. In production, use a secret store (Azure Key Vault, AWS Secrets Manager, etc.).

## ⚠️ Before sharing the link (checklist)

Things a senior dev will notice when cloning or commenting on the post. This repo documents them on purpose.

### 🔴 SecretKey hardcoded in the repo

**Issue:** committing a real key in `appsettings.json` contradicts any post about *secret management*.

**Status in this repo:** no real key is **committed**. `appsettings.json` uses a placeholder; the local key is set via User Secrets or environment variables (see above).

**Before publishing on LinkedIn, verify:**

- [ ] No real keys in `appsettings.json` or `appsettings.Development.json`
- [ ] `UserSecretsId` is present in `jwt-example.csproj` (or run `dotnet user-secrets init` before pushing)
- [ ] The README explains how to configure `Jwt:SecretKey` locally
- [ ] If you ever committed a key by mistake, rotate it before publishing

### 🟡 Login does not validate credentials

**Issue:** `POST /usuarios/login` accepts any `username`/`password` and always issues the same JWT.

**Status in this repo:** **intentional** for this educational demo. The code states it explicitly:

```csharp
// NOTE: Credential validation omitted intentionally.
// In production, validate against your user store here.
```

**Before publishing, make it clear in the post** that the example demonstrates the **JWT flow** (generate → send → validate), not a full authentication system.

## Endpoints

### `POST /usuarios/login`

Generates a JWT and returns it to the client.

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

**Claims included in the token**

| Claim | Example value |
|-------|---------------|
| `sub` | `123` |
| `email` | `user@test.com` |
| `role` | `Admin` |

**Notes**

- Credentials in the body are **not validated** in this demo; the endpoint always issues the same sample token.
- Issuer, audience, and expiration are taken from JWT configuration.

---

### `GET /usuarios/test`

Protected endpoint. Requires a valid JWT.

**Request**

```http
GET /usuarios/test
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response `200 OK`**

```json
{
  "message": "Valid token",
  "claims": [
    { "type": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "value": "123" },
    { "type": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", "value": "user@test.com" },
    { "type": "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "value": "Admin" }
  ]
}
```

**Response `401 Unauthorized`**

Returned when:

- The `Authorization` header is missing.
- The header is not in `Bearer <token>` format.
- The token is malformed, expired, has an invalid signature, or wrong issuer/audience.

## Token validation

Validation is performed by the **JWT Bearer** middleware configured in `Program.cs`. A value in the header is not enough — all of the following are checked:

| Validation | Configuration |
|------------|---------------|
| Signature | `ValidateIssuerSigningKey = true` with `SecretKey` |
| Issuer | `ValidateIssuer = true`, `ValidIssuer = "mi-api"` |
| Audience | `ValidateAudience = true`, `ValidAudience = "mi-frontend"` |
| Expiration | `ValidateLifetime = true` |

Relevant middleware pipeline:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

The `[Authorize]` attribute on `/usuarios/test` requires successful authentication. It does **not** validate roles by itself; any valid token is enough.

To require a specific role:

```csharp
[Authorize(Roles = "Admin")]
```

## How to test

### Recommended flow

1. Run the API with `dotnet run`.
2. Call `POST /usuarios/login` and copy the full `token` value.
3. Call `GET /usuarios/test` with:

   ```http
   Authorization: Bearer <full_token>
   ```

### Common mistakes

| Mistake | Cause |
|---------|-------|
| `401 Unauthorized` | Wrong header or invalid token |
| Using `Bearer <token>` as the header name | The header name is `Authorization`, not `Bearer` |
| Sending the `SecretKey` instead of the JWT | Send the token returned by `/login` |
| Truncated token | A JWT has three dot-separated parts |

### curl example

```bash
# 1. Login
curl -k -X POST https://localhost:7294/usuarios/login \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"demo\",\"password\":\"demo\"}"

# 2. Protected endpoint (replace TOKEN)
curl -k https://localhost:7294/usuarios/test \
  -H "Authorization: Bearer TOKEN"
```

### `jwt-example.http` example

```http
@host = https://localhost:7294

### Login
POST {{host}}/usuarios/login
Content-Type: application/json

{
  "username": "demo",
  "password": "demo"
}

### Protected endpoint
@token = paste_login_token_here

GET {{host}}/usuarios/test
Authorization: Bearer {{token}}
```

## Project structure

```
jwt-example/
├── Controllers/
│   └── UsuariosController.cs   # Login and protected endpoint
├── JWT/
│   └── JwtOptions.cs           # JWT configuration model
├── Model/
│   ├── LoginViewModel.cs       # Login request
│   └── LoginResponse.cs        # Token response
├── Program.cs                  # Auth setup and pipeline
├── appsettings.json
└── appsettings.Development.json
```

## Main dependencies

- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Microsoft.IdentityModel.JsonWebTokens`
- `Microsoft.AspNetCore.OpenApi`
- `Scalar.AspNetCore` (Development documentation)

## Demo limitations

This project is meant to **demonstrate the JWT mechanism**, not as a production baseline:

- No real username/password validation.
- Token claims are hardcoded.
- No refresh tokens or token revocation.
- `[Authorize]` validates identity, not role-based permissions.
- Does not cover HS256 vs RS256 — this example uses **HS256** (symmetric). See the LinkedIn post for RS256/ES256 comparison.

## License

Educational sample project.
