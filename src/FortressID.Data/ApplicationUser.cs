using Microsoft.AspNetCore.Identity;

namespace FortressID.Data;

/// <summary>
/// Representa un usuario de la aplicación en el sistema de identidad.
/// Hereda de IdentityUser para aprovechar la infraestructura de seguridad de ASP.NET Core Identity,
/// incluyendo gestión de contraseñas hasheadas, bloqueo de cuentas, confirmación de email y autenticación multifactor.
/// </summary>
/// <remarks>
/// Esta clase puede extenderse con propiedades personalizadas según los requisitos del dominio.
/// Por ejemplo: identificador de empleado, fecha de creación, departamento, etc.
/// </remarks>
public class ApplicationUser : IdentityUser
{
}