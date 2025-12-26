using System.ComponentModel.DataAnnotations;

namespace FortressID.Server.ViewModels;

/// <summary>
/// Modelo de vista para el formulario de inicio de sesión.
/// Captura las credenciales del usuario y preserva la URL de retorno para redirigir tras la autenticación exitosa.
/// </summary>
public class LoginViewModel
{
    /// <summary>
    /// Dirección de correo electrónico del usuario. Debe ser única en el sistema.
    /// </summary>
    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña del usuario. Se valida contra el hash almacenado en Identity.
    /// </summary>
    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// URL de destino después de la autenticación exitosa.
    /// Preserva el contexto de la solicitud OAuth2 original para completar el flujo de autorización.
    /// </summary>
    public string? ReturnUrl { get; set; }
}