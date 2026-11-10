using System.Globalization;
using AdmitUWA.Components;
using AdmitUWA.Data;
using AdmitUWA.Services;

// Fechas y números en español del Perú, sin depender de la configuración del servidor.
CultureInfo.DefaultThreadCurrentCulture = Formato.Peru;
CultureInfo.DefaultThreadCurrentUICulture = Formato.Peru;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Cascarón de interfaz: los datos viven en la sesión (DatosSesion) hasta integrar la API Java.
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<DatosSesion>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
