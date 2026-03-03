using Chummer.Blazor.Components;
using Chummer.Presentation;
using Chummer.Presentation.Overview;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<IChummerClient, HttpChummerClient>((_, client) =>
{
    string? configuredBaseUrl = builder.Configuration["Chummer:ApiBaseUrl"];
    string? environmentBaseUrl = Environment.GetEnvironmentVariable("CHUMMER_API_BASE_URL");
    string baseUrl = configuredBaseUrl
        ?? environmentBaseUrl
        ?? "http://chummer-web:8080";

    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddScoped<ICharacterOverviewPresenter, CharacterOverviewPresenter>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
