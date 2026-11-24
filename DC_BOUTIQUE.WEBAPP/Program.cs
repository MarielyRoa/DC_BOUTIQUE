using DC_BOUTIQUE.Core.Application;
using DC_BOUTIQUE.Infrastructure.Identity;
using DC_BOUTIQUE.Infrastructure.Persistence;
using DC_BOUTIQUE.Infrastructure.Shared;
using DC_BOUTIQUE.WEBAPP.Extensions;
using DC_BOUTIQUE.WEBAPP.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Evitar doble instancia si ya está abierto en modo instalado (abre la ventana existente en vez de fallar el puerto)
bool isNewInstance = true;
using var singleInstanceMutex = !builder.Environment.IsDevelopment()
    ? new Mutex(true, "DC_BOUTIQUE_LOCAL_DESKTOP_INSTANCE", out isNewInstance)
    : null;
if (!isNewInstance)
{
    ApplicationStartupExtensions.OpenDesktopWindow("http://localhost:5055");
    return;
}

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
