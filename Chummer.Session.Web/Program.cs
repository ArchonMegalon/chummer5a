using Chummer.Session.Web;
using Chummer.Session.Web.Components;

var builder = WebApplication.CreateBuilder(args);
string? configuredPathBase = builder.Configuration["Chummer:PathBase"];
string? environmentPathBase = Environment.GetEnvironmentVariable("CHUMMER_SESSION_PATH_BASE");
PathString pathBase = NormalizePathBase(configuredPathBase ?? environmentPathBase);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddScoped<BrowserSessionApiClient>();
builder.Services.AddScoped<BrowserSessionCoachApiClient>();
global::Chummer.Infrastructure.Browser.DependencyInjection.BrowserServiceCollectionExtensions.AddBrowserSessionOfflineStorage(builder.Services);

var app = builder.Build();

if (pathBase.HasValue)
{
    app.UsePathBase(pathBase);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/health", () => Results.Ok(new
{
    ok = true,
    head = "session-web",
    pathBase = pathBase.Value,
    utc = DateTimeOffset.UtcNow
}));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static PathString NormalizePathBase(string? rawPathBase)
{
    if (string.IsNullOrWhiteSpace(rawPathBase))
    {
        return PathString.Empty;
    }

    string normalized = rawPathBase.Trim();
    if (!normalized.StartsWith("/", StringComparison.Ordinal))
    {
        normalized = "/" + normalized;
    }

    if (normalized.Length > 1 && normalized.EndsWith("/", StringComparison.Ordinal))
    {
        normalized = normalized.TrimEnd('/');
    }

    return normalized == "/" ? PathString.Empty : new PathString(normalized);
}
