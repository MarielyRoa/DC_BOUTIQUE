using DC_BOUTIQUE.Core.Application;
using DC_BOUTIQUE.Infrastructure.Identity;
using DC_BOUTIQUE.Infrastructure.Persistence;
using DC_BOUTIQUE.Infrastructure.Shared;
using DC_BOUTIQUE.WEBAPP.Extensions;
using DC_BOUTIQUE.WEBAPP.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración local y registro de capas (Arquitectura Onion)
builder.ConfigureLocalDesktopHost(args);
builder.Services.AddWebAppServices();
builder.Services.AddApplicationLayer();
builder.Services.AddPersistenceInfrastructure(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddSharedInfrastructure(builder.Configuration);

var app = builder.Build();

// 2. Inicialización automática de SQLite local en ProgramData
await app.InitializeLocalDatabaseAsync();

// 3. Pipeline HTTP
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseExceptionHandler("/Home/Error");

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseLocalStaticStorage();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.LaunchBrowserOnLocalStart();

await app.RunAsync();
