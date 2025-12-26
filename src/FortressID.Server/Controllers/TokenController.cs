using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Microsoft.AspNetCore.Authentication;
using OpenIddict.Abstractions;

namespace FortressID.Server.Controllers;

/// <summary>
/// Controlador que implementa el endpoint de intercambio de tokens OAuth2.
/// Intercambia códigos de autorización y refresh tokens por access tokens y refresh tokens nuevos.
/// </summary>
public class TokenController : Controller
{
    private readonly IOpenIddictApplicationManager _applicationManager;

    /// <summary>
    /// Inicializa una nueva instancia del controlador de tokens.
    /// </summary>
    /// <param name="applicationManager">Gestor de aplicaciones cliente OAuth2.</param>
    public TokenController(IOpenIddictApplicationManager applicationManager)
    {
        _applicationManager = applicationManager;
    }

    /// <summary>
    /// Endpoint de intercambio de tokens que procesa solicitudes de tipo Authorization Code y Refresh Token.
    /// Valida el código o refresh token recibido y emite nuevos tokens de acceso con los claims del usuario.
    /// </summary>
    /// <returns>
    /// SignIn con nuevos access tokens y refresh tokens si la validación es exitosa.
    /// Forbid con error OAuth2 si el código o refresh token es inválido o ha expirado.
    /// </returns>
    [HttpPost("~/connect/token")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("Solicitud inválida.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            
            if (result.Principal == null)
            {
                 return Forbid(
                     authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                     properties: new AuthenticationProperties(new Dictionary<string, string?>
                     {
                         [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                         [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "El token ya no es válido."
                     }));
            }

            return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException("El tipo de grant no está soportado.");
    }
}