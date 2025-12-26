using FortressID.Data;
using FortressID.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// =================================================================
// 1. CONFIGURACIÓN DE BASE DE DATOS
// =================================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseOpenIddict();
});

// =================================================================
// 2. CONFIGURACIÓN DE IDENTITY (USUARIOS Y COOKIES)
// =================================================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.User.RequireUniqueEmail = true;
});

// ⚠️ ADVERTENCIA DE SEGURIDAD: CONFIGURACIÓN EXCLUSIVA PARA DESARROLLO
// CookieSecurePolicy.SameAsRequest permite cookies en HTTP (localhost).
// En producción, configurar CookieSecurePolicy.Always y usar HTTPS obligatorio.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = "FortressID.Auth";
});

// =================================================================
// 3. CONFIGURACIÓN DE OPENIDDICT
// =================================================================
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token");

        options.AllowAuthorizationCodeFlow()
               .AllowRefreshTokenFlow();

        options.RequireProofKeyForCodeExchange();

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        options.RegisterScopes(
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Roles,
            OpenIddictConstants.Scopes.OfflineAccess
        );

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               // ⚠️ ADVERTENCIA DE SEGURIDAD: CONFIGURACIÓN EXCLUSIVA PARA DESARROLLO
               // DisableTransportSecurityRequirement permite conexiones HTTP (localhost).
               // En producción, eliminar esta línea y configurar HTTPS obligatorio.
               .DisableTransportSecurityRequirement();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// =================================================================
// 4. CONTROLADORES Y WORKER
// =================================================================
// ⚠️ ADVERTENCIA DE SEGURIDAD: CONFIGURACIÓN EXCLUSIVA PARA DESARROLLO
// IgnoreAntiforgeryTokenAttribute desactiva la protección CSRF globalmente.
// En producción, eliminar este filtro y habilitar validación de tokens antifalsificación.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryTokenAttribute());
});

builder.Services.AddHostedService<Worker>();

var app = builder.Build();

// =================================================================
// 5. PIPELINE HTTP
// =================================================================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultControllerRoute();

app.Run();