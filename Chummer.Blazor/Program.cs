using Chummer.Blazor.Components;
using Chummer.Presentation;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;
using Chummer.Rulesets.Sr5;

var builder = WebApplication.CreateBuilder(args);
string? configuredPathBase = builder.Configuration["Chummer:PathBase"];
string? environmentPathBase = Environment.GetEnvironmentVariable("CHUMMER_BLAZOR_PATH_BASE");
PathString pathBase = NormalizePathBase(configuredPathBase ?? environmentPathBase);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<IChummerClient, HttpChummerClient>((_, client) =>
{
    string? configuredBaseUrl = builder.Configuration["Chummer:ApiBaseUrl"];
    string? environmentBaseUrl = Environment.GetEnvironmentVariable("CHUMMER_API_BASE_URL");
    string? configuredApiKey = builder.Configuration["Chummer:ApiKey"];
    string? environmentApiKey = Environment.GetEnvironmentVariable("CHUMMER_API_KEY");
    string baseUrl = configuredBaseUrl
        ?? environmentBaseUrl
        ?? "http://chummer-api:8080";
    string? apiKey = configuredApiKey ?? environmentApiKey;

    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(20);
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        client.DefaultRequestHeaders.Remove("X-Api-Key");
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }
});

builder.Services.AddScoped<ICharacterOverviewPresenter, CharacterOverviewPresenter>();
builder.Services.AddScoped<IShellPresenter, ShellPresenter>();
builder.Services.AddScoped<IShellBootstrapDataProvider, ShellBootstrapDataProvider>();
builder.Services.AddChummerRulesets();
builder.Services.AddSingleton<ICommandAvailabilityEvaluator, DefaultCommandAvailabilityEvaluator>();
builder.Services.AddSingleton<IShellSurfaceResolver, ShellSurfaceResolver>();

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
    head = "blazor",
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
