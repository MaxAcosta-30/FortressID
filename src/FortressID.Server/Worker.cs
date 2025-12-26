using FortressID.Data;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace FortressID.Server;

/// <summary>
/// Servicio de inicialización que ejecuta tareas de seeding al arrancar la aplicación.
/// Crea la base de datos si no existe, registra aplicaciones cliente OAuth2 y crea usuarios de prueba.
/// </summary>
/// <remarks>
/// Este servicio utiliza un scope manual porque los servicios HostedService son Singleton,
/// mientras que DbContext y otros servicios relacionados con Identity son Scoped.
/// </remarks>
public class Worker : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Worker> _logger;

    /// <summary>
    /// Inicializa una nueva instancia del servicio de inicialización.
    /// </summary>
    /// <param name="serviceProvider">Proveedor de servicios para crear scopes.</param>
    /// <param name="logger">Logger para eventos de inicialización.</param>
    public Worker(IServiceProvider serviceProvider, ILogger<Worker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta las tareas de inicialización: creación de base de datos, registro de clientes OAuth2 y creación de usuarios de semilla.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        await context.Database.EnsureCreatedAsync(cancellationToken);
        _logger.LogInformation("Base de datos inicializada o verificada.");

        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (await manager.FindByClientIdAsync("postman", cancellationToken) is null)
        {
            _logger.LogInformation("Registrando cliente OAuth2 'postman' para pruebas.");

            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "postman",
                ClientSecret = "postman-secret",
                DisplayName = "Postman Client",
                RedirectUris = { new Uri("https://oauth.pstmn.io/v1/callback") },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles
                }
            }, cancellationToken);
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        if (await userManager.FindByEmailAsync("admin@fortress.local") is null)
        {
            _logger.LogInformation("Creando usuario administrador de semilla.");
            var user = new ApplicationUser 
            { 
                UserName = "admin@fortress.local", 
                Email = "admin@fortress.local",
                EmailConfirmed = true 
            };
            
            await userManager.CreateAsync(user, "Fortress@2025!"); 
            _logger.LogWarning("Usuario administrador creado con credenciales por defecto. CAMBIAR EN PRODUCCIÓN.");
        }
    }

    /// <summary>
    /// Método invocado al detener el servicio. No requiere limpieza adicional.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}