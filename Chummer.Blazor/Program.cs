using Chummer.Blazor.Components;
using Chummer.Presentation;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSingleton<ICommandAvailabilityEvaluator, DefaultCommandAvailabilityEvaluator>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/health", () => Results.Ok(new
{
    ok = true,
    head = "blazor",
    utc = DateTimeOffset.UtcNow
}));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
