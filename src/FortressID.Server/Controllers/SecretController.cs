using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;

namespace FortressID.Server.Controllers;

/// <summary>
/// Controlador de ejemplo que demuestra la protección de endpoints mediante tokens de acceso OAuth2.
/// Requiere autenticación mediante el esquema de validación de OpenIddict.
/// </summary>
[ApiController]
[Route("api/secret")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class SecretController : ControllerBase
{
    /// <summary>
    /// Endpoint protegido que retorna información sensible solo accesible con un token de acceso válido.
    /// Demuestra la validación de tokens JWT emitidos por el servidor de autorización.
    /// </summary>
    /// <returns>
    /// Objeto JSON con mensaje de éxito, identidad del usuario autenticado y sus claims del token.
    /// </returns>
    [HttpGet]
    public IActionResult GetSecretMessage()
    {
        var user = User.Identity?.Name;
        var claims = User.Claims.Select(c => new { c.Type, c.Value });

        return Ok(new 
        { 
            Message = "Acceso concedido a la bóveda",
            User = user,
            SecretData = "La fórmula de la Coca-Cola es: [REDACTED]",
            YourClaims = claims
        });
    }
}