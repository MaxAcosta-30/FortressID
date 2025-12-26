using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using FortressID.Data;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace FortressID.Server.Controllers;

/// <summary>
/// Controlador que implementa el endpoint de autorización OAuth2/OpenID Connect.
/// Gestiona el flujo Authorization Code con PKCE, generando códigos de autorización tras la autenticación del usuario.
/// </summary>
public class AuthorizationController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    /// <summary>
    /// Inicializa una nueva instancia del controlador de autorización.
    /// </summary>
    /// <param name="signInManager">Gestor de autenticación de Identity.</param>
    /// <param name="userManager">Gestor de usuarios de Identity.</param>
    public AuthorizationController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    /// <summary>
    /// Endpoint de autorización OAuth2 que genera códigos de autorización tras validar la autenticación del usuario.
    /// Si el usuario no está autenticado, redirige al flujo de login preservando los parámetros de la solicitud OAuth2.
    /// Si está autenticado, crea una identidad con claims del usuario y emite un código de autorización firmado.
    /// </summary>
    /// <returns>
    /// Challenge (redirección a login) si el usuario no está autenticado.
    /// SignIn con código de autorización si la autenticación es exitosa.
    /// </returns>
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("No se pudo recuperar la solicitud OpenID.");

        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        if (!result.Succeeded)
        {
            return Challenge(
                authenticationSchemes: IdentityConstants.ApplicationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + QueryString.Create(
                        Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList())
                });
        }

        var user = await _userManager.GetUserAsync(result.Principal) ??
            throw new InvalidOperationException("El usuario no existe.");

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user));
        identity.SetClaim(Claims.Name, await _userManager.GetUserNameAsync(user));

        identity.SetDestinations(GetDestinations);

        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Determina en qué tokens (Access Token, Identity Token) debe incluirse cada claim.
    /// El Subject claim se incluye en ambos tokens para permitir la identificación del usuario.
    /// </summary>
    /// <param name="claim">Claim a evaluar.</param>
    /// <returns>Colección de destinos donde debe incluirse el claim.</returns>
    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        if (claim.Type == Claims.Subject)
        {
            yield return Destinations.AccessToken;
            yield return Destinations.IdentityToken;
        }
        else
        {
            yield return Destinations.AccessToken;
        }
    }
}