# FortressID - Servidor de Identidad Centralizado

Servidor de autorización OAuth2/OpenID Connect implementado con OpenIddict y ASP.NET Core Identity sobre .NET 10. Proporciona autenticación y autorización centralizada mediante el flujo Authorization Code con PKCE y soporte para Refresh Tokens.

## Arquitectura

### Tecnologías Principales

- **.NET 10.0**: Framework de aplicación
- **ASP.NET Core Identity**: Gestión de usuarios, contraseñas y autenticación basada en cookies
- **OpenIddict 7.2.0**: Servidor de autorización OAuth2/OpenID Connect
- **Entity Framework Core 10.0.1**: ORM para persistencia de datos
- **SQL Server**: Base de datos relacional (LocalDB para desarrollo)

### Patrones de Seguridad Implementados

- **PKCE (Proof Key for Code Exchange)**: Protección contra interceptación de códigos de autorización en clientes públicos
- **Authorization Code Flow**: Flujo estándar OAuth2 para aplicaciones web y móviles
- **Refresh Tokens**: Renovación de tokens de acceso sin requerir nueva autenticación del usuario
- **Password Policies**: Contraseñas con requisitos de complejidad (12 caracteres mínimo, mayúsculas, números, caracteres especiales)
- **Account Lockout**: Bloqueo temporal de cuentas tras múltiples intentos fallidos de autenticación
- **HttpOnly Cookies**: Protección contra acceso JavaScript a cookies de autenticación
- **SameSite Cookies**: Mitigación de ataques CSRF mediante restricción de envío de cookies en solicitudes cross-site

### Estructura del Proyecto

```
FortressID/
├── src/
│   ├── FortressID.Data/          # Capa de acceso a datos (DbContext, Identity)
│   └── FortressID.Server/         # Aplicación web (Controladores, Views, Worker)
├── tests/
│   └── FortressID.Tests/          # Proyecto de pruebas unitarias
└── README.md
```

## Prerrequisitos

- **.NET SDK 10.0** o superior
- **SQL Server LocalDB** (incluido con Visual Studio) o SQL Server Express
- **Visual Studio 2022** o **Visual Studio Code** con extensión C# (opcional)

## Guía de Despliegue Local

### 1. Clonar el Repositorio

```bash
git clone <url-del-repositorio>
cd FortressID
```

### 2. Restaurar Dependencias

```bash
dotnet restore
```

### 3. Configurar Base de Datos

La cadena de conexión se encuentra en `src/FortressID.Server/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FortressID_DB;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

**Nota**: Ajuste la cadena de conexión según su entorno si no utiliza LocalDB.

### 4. Inicializar Base de Datos

La aplicación crea automáticamente la base de datos al iniciar mediante `EnsureCreatedAsync()`. Si prefiere usar migraciones de Entity Framework:

```bash
cd src/FortressID.Server
dotnet ef migrations add InitialCreate --project ../FortressID.Data
dotnet ef database update --project ../FortressID.Data
```

### 5. Ejecutar la Aplicación

```bash
cd src/FortressID.Server
dotnet run
```

La aplicación estará disponible en `http://localhost:5000` (o el puerto configurado en `launchSettings.json`).

## Datos de Semilla (Seeding)

Al iniciar la aplicación, el servicio `Worker` ejecuta automáticamente las siguientes tareas de inicialización:

### Usuario Administrador

- **Email**: `admin@fortress.local`
- **Contraseña**: `Fortress@2025!`
- **Estado**: Email confirmado, cuenta activa

**ADVERTENCIA DE SEGURIDAD**: Estas credenciales son exclusivas para desarrollo. En producción, elimine o modifique el código de seeding y cambie las credenciales por defecto inmediatamente después del despliegue.

### Cliente OAuth2: Postman

Se registra automáticamente un cliente OAuth2 con las siguientes características:

- **Client ID**: `postman`
- **Client Secret**: `postman-secret`
- **Redirect URI**: `https://oauth.pstmn.io/v1/callback`
- **Grant Types**: Authorization Code, Refresh Token
- **Scopes**: `email`, `profile`, `roles`, `offline_access`

Este cliente está configurado para facilitar pruebas manuales con Postman. En producción, registre clientes mediante un proceso administrativo seguro.

## Endpoints

### Endpoints OAuth2/OpenID Connect

- **Authorization Endpoint**: `GET/POST /connect/authorize`
  - Genera códigos de autorización tras la autenticación del usuario
  - Requiere autenticación previa mediante cookies de Identity

- **Token Endpoint**: `POST /connect/token`
  - Intercambia códigos de autorización por access tokens y refresh tokens
  - Intercambia refresh tokens por nuevos access tokens

### Endpoints de Aplicación

- **Login**: `GET /Account/Login`
  - Presenta el formulario de inicio de sesión
  - Acepta parámetro `returnUrl` para preservar el contexto OAuth2

- **API Protegida**: `GET /api/secret`
  - Endpoint de ejemplo que requiere un access token válido
  - Demuestra la validación de tokens JWT mediante OpenIddict Validation

## Ejemplo de Uso: Solicitud de Token

### Paso 1: Obtener Código de Autorización

Redirija al usuario al endpoint de autorización:

```
GET /connect/authorize?
  client_id=postman&
  redirect_uri=https://oauth.pstmn.io/v1/callback&
  response_type=code&
  scope=openid email profile roles offline_access&
  code_challenge=<PKCE_CODE_CHALLENGE>&
  code_challenge_method=S256
```

Si el usuario no está autenticado, será redirigido a `/Account/Login`. Tras la autenticación exitosa, será redirigido de vuelta al endpoint de autorización y recibirá un código de autorización en el callback.

### Paso 2: Intercambiar Código por Token

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code&
code=<AUTHORIZATION_CODE>&
redirect_uri=https://oauth.pstmn.io/v1/callback&
client_id=postman&
client_secret=postman-secret&
code_verifier=<PKCE_CODE_VERIFIER>
```

Respuesta:

```json
{
  "access_token": "<JWT_ACCESS_TOKEN>",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "<REFRESH_TOKEN>",
  "scope": "openid email profile roles offline_access"
}
```

### Paso 3: Acceder a API Protegida

```http
GET /api/secret
Authorization: Bearer <JWT_ACCESS_TOKEN>
```

Respuesta:

```json
{
  "message": "Acceso concedido a la bóveda",
  "user": "admin@fortress.local",
  "secretData": "La fórmula de la Coca-Cola es: [REDACTED]",
  "yourClaims": [
    {
      "type": "sub",
      "value": "<USER_ID>"
    },
    {
      "type": "name",
      "value": "admin@fortress.local"
    }
  ]
}
```

### Paso 4: Renovar Token con Refresh Token

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token&
refresh_token=<REFRESH_TOKEN>&
client_id=postman&
client_secret=postman-secret
```

## Configuración de Producción

### Cambios Requeridos

1. **HTTPS Obligatorio**: Elimine `.DisableTransportSecurityRequirement()` en `Program.cs` y configure HTTPS en el servidor.

2. **Cookies Seguras**: Cambie `CookieSecurePolicy.SameAsRequest` a `CookieSecurePolicy.Always` en `Program.cs`.

3. **Protección CSRF**: Elimine el filtro `IgnoreAntiforgeryTokenAttribute` y habilite la validación de tokens antifalsificación.

4. **Certificados de Producción**: Reemplace `AddDevelopmentEncryptionCertificate()` y `AddDevelopmentSigningCertificate()` por certificados de producción.

5. **Gestión de Secretos**: Configure `ClientSecret` mediante variables de entorno o un sistema de gestión de secretos (Azure Key Vault, AWS Secrets Manager, etc.).

6. **Base de Datos**: Utilice una instancia de SQL Server en producción con cadena de conexión segura.

7. **Logging**: Configure un sistema de logging centralizado (Application Insights, Serilog con sinks de producción, etc.).

## Licencia

Este proyecto es de uso educativo y demostrativo.

