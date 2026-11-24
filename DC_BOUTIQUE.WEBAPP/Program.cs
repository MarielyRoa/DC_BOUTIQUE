using DC_BOUTIQUE.Core.Application;
using DC_BOUTIQUE.Infrastructure.Identity;
using DC_BOUTIQUE.Infrastructure.Identity.Contexts;
using DC_BOUTIQUE.Infrastructure.Identity.Entities;
using DC_BOUTIQUE.Infrastructure.Identity.Seeds;
using DC_BOUTIQUE.Infrastructure.Persistence;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using DC_BOUTIQUE.Infrastructure.Shared;
using DC_BOUTIQUE.WEBAPP.Middlewares;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. CONFIGURACIÓN DE SERVICIOS
// =========================================================================

// Controladores y Vistas MVC
builder.Services.AddControllersWithViews();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// Gestión de Sesión HTTP (Turno de 8 horas)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".DC_BOUTIQUE.Session";
});

// Acceso al contexto HTTP y validación de sesión
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<ValidateUserSession>();

// Registro de capas de la Arquitectura Onion
builder.Services.AddApplicationLayer();
builder.Services.AddPersistenceInfrastructure(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddSharedInfrastructure(builder.Configuration);

var app = builder.Build();

// =========================================================================
// 2. INICIALIZACIÓN Y MIGRACIÓN AUTOMÁTICA DE BASE DE DATOS
// =========================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // 2.1 Dominio y Negocio (Migraciones y Configuración Inicial)
        var context = services.GetRequiredService<DCBoutiqueContext>();
        await DbInitializer.SeedAsync(context);

        // 2.2 Capa de Identity (Migraciones y Roles/Usuarios Base)
        var identityContext = services.GetRequiredService<IdentityContext>();
        if (identityContext.Database.IsSqlite())
        {
            await identityContext.Database.EnsureCreatedAsync();
        }
        else
        {
            await identityContext.Database.MigrateAsync();
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        await DefaultUserRoles.SeedAsync(roleManager);
        await DefaultAdminUser.SeedAsync(userManager);
        await DefaultCashierUser.SeedAsync(userManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al inicializar la base de datos.");
    }
}

// =========================================================================
// 3. PIPELINE DE PETICIONES HTTP
// =========================================================================
// Middleware global para captura funcional de excepciones en API y Navegación
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseExceptionHandler("/Home/Error");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Middleware de Sesión y Seguridad
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Ruta de inicio predeterminada (Login)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

await app.RunAsync();
