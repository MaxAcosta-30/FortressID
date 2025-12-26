using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FortressID.Data;

/// <summary>
/// Contexto de base de datos que integra ASP.NET Core Identity y OpenIddict.
/// Hereda de IdentityDbContext para obtener automáticamente las tablas de usuarios, roles, claims y tokens de Identity.
/// OpenIddict inyecta sus propias entidades (aplicaciones, autorizaciones, tokens) mediante la configuración en Program.cs.
/// </summary>
/// <remarks>
/// Es crítico llamar a base.OnModelCreating(builder) para que Identity configure correctamente sus tablas.
/// Sin esta llamada, la aplicación fallará al inicializar debido a claves primarias no configuradas.
/// </remarks>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    /// <summary>
    /// Inicializa una nueva instancia del contexto de base de datos.
    /// </summary>
    /// <param name="options">Opciones de configuración del contexto, incluyendo la cadena de conexión.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Configura el modelo de datos para Identity y permite personalizaciones adicionales.
    /// </summary>
    /// <param name="builder">Constructor del modelo de Entity Framework Core.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}