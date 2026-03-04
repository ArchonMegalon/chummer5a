using Chummer.Presentation;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;

namespace Chummer.Blazor.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        appBuilder.Services.AddLogging();
        appBuilder.Services.AddSingleton(CreateApiHttpClient());
        appBuilder.Services.AddSingleton<IChummerClient, HttpChummerClient>();
        appBuilder.Services.AddSingleton<ICharacterOverviewPresenter, CharacterOverviewPresenter>();
        appBuilder.Services.AddSingleton<IShellPresenter, ShellPresenter>();
        appBuilder.Services.AddSingleton<ICommandAvailabilityEvaluator, DefaultCommandAvailabilityEvaluator>();
        appBuilder.Services.AddSingleton<Chummer.Blazor.CharacterOverviewStateBridge>();

        appBuilder.RootComponents.Add<App>("app");

        PhotinoBlazorApp app = appBuilder.Build();
        app.MainWindow
            .SetTitle("Chummer Blazor Desktop")
            .SetUseOsDefaultSize(false)
            .SetSize(1440, 960)
            .SetResizable(true)
            .SetMaximized(false);

        app.Run();
    }

    private static HttpClient CreateApiHttpClient()
    {
        HttpClient client = new()
        {
            BaseAddress = ResolveApiBaseAddress(),
            Timeout = TimeSpan.FromSeconds(20)
        };

        string? apiKey = ResolveApiKey();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.Remove("X-Api-Key");
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        return client;
    }

    private static Uri ResolveApiBaseAddress()
    {
        string? configured = Environment.GetEnvironmentVariable("CHUMMER_API_BASE_URL");
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = "http://127.0.0.1:8088";
        }

        if (!Uri.TryCreate(configured, UriKind.Absolute, out Uri? uri))
        {
            throw new InvalidOperationException($"Invalid CHUMMER_API_BASE_URL: '{configured}'");
        }

        return uri;
    }

    private static string? ResolveApiKey()
    {
        return Environment.GetEnvironmentVariable("CHUMMER_API_KEY");
    }
}
