using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FortressID.Data;
using FortressID.Server.ViewModels;

namespace FortressID.Server.Controllers;

/// <summary>
/// Controlador responsable de la autenticación de usuarios mediante formularios web.
/// Gestiona el flujo de inicio de sesión que precede a la autorización OAuth2/OpenID Connect.
/// </summary>
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;

    /// <summary>
    /// Inicializa una nueva instancia del controlador de cuentas.
    /// </summary>
    /// <param name="signInManager">Gestor de autenticación de Identity.</param>
    /// <param name="userManager">Gestor de usuarios de Identity.</param>
    /// <param name="logger">Logger para eventos de seguridad y auditoría.</param>
    public AccountController(
        SignInManager<ApplicationUser> signInManager, 
        UserManager<ApplicationUser> userManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Presenta el formulario de inicio de sesión.
    /// Este endpoint es el punto de entrada para usuarios no autenticados que intentan acceder a recursos protegidos.
    /// </summary>
    /// <param name="returnUrl">URL de destino después de la autenticación exitosa. Se preserva durante el flujo OAuth2.</param>
    /// <returns>Vista de login.</returns>
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    /// <summary>
    /// Procesa el intento de autenticación del usuario.
    /// Valida credenciales contra Identity y establece la sesión mediante cookies.
    /// Implementa bloqueo de cuenta tras múltiples intentos fallidos para mitigar ataques de fuerza bruta.
    /// </summary>
    /// <param name="model">Modelo con credenciales del usuario (email y contraseña).</param>
    /// <returns>
    /// Redirección al returnUrl si la autenticación es exitosa.
    /// Vista de login con errores si falla la validación o las credenciales son incorrectas.
    /// Vista de bloqueo si la cuenta está temporalmente bloqueada.
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            _logger.LogWarning("Intento de login fallido: usuario no encontrado. Email: {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("Login exitoso para el usuario: {UserId}", user.Id);
            return LocalRedirect(model.ReturnUrl ?? "/");
        }
        
        if (result.IsLockedOut)
        {
            _logger.LogWarning("Intento de login bloqueado: cuenta bloqueada. Usuario: {UserId}", user.Id);
            return View("Lockout");
        }
        
        if (result.IsNotAllowed)
        {
            _logger.LogWarning("Intento de login rechazado: cuenta no permitida. Usuario: {UserId}", user.Id);
            ModelState.AddModelError(string.Empty, "Su cuenta requiere verificación adicional.");
        }
        else
        {
            _logger.LogWarning("Intento de login fallido: contraseña incorrecta. Usuario: {UserId}", user.Id);
            ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
        }

        return View(model);
    }
}