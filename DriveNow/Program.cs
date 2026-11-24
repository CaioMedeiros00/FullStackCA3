using DriveNow.Api;
using DriveNow.Auth;
using DriveNow.Components;
using DriveNow.Services;
using DriveNow.State;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Blazor (Interactive Server render mode)
// ---------------------------------------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ---------------------------------------------------------------------------
// API configuration
// By default the app talks to the egolance.com REST APIs required by the brief.
// Set "ApiSettings:UseLocalApi" to true to use the built-in in-memory API
// (see Api/LocalApiEndpoints.cs), which implements the same contract.
// ---------------------------------------------------------------------------
var apiSettings = builder.Configuration.GetSection("ApiSettings");
var useLocalApi = apiSettings.GetValue<bool>("UseLocalApi");
var apiBaseUrl = (useLocalApi ? apiSettings["LocalBaseUrl"] : apiSettings["BaseUrl"])
    ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured in appsettings.json.");
if (!apiBaseUrl.EndsWith('/'))
{
    apiBaseUrl += "/";
}
var timeoutSeconds = apiSettings.GetValue("TimeoutSeconds", 15);

void ConfigureApiClient(HttpClient client)
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    client.DefaultRequestHeaders.Accept.Add(new("application/json"));
}

// Typed HttpClients - one service per API, all communication is centralised here.
builder.Services.AddHttpClient<CarService>(ConfigureApiClient);
builder.Services.AddHttpClient<BookingService>(ConfigureApiClient);
builder.Services.AddHttpClient<LocationService>(ConfigureApiClient);

// ---------------------------------------------------------------------------
// Authentication & authorization (custom AuthenticationStateProvider +
// hardcoded users + browser session storage)
// ---------------------------------------------------------------------------
builder.Services.AddScoped<DriveNowAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<DriveNowAuthStateProvider>());
builder.Services.AddScoped<AuthService>();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// ---------------------------------------------------------------------------
// Application state (scoped = one instance per user circuit)
// ---------------------------------------------------------------------------
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<ThemeService>();

// Local in-memory API data store (only used when UseLocalApi = true)
builder.Services.AddSingleton<InMemoryDataStore>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapDriveNowLocalApi();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
