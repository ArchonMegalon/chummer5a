using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Chummer.Contracts.Presentation;
using Chummer.Presentation;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chummer.Avalonia;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _serviceProvider = BuildServiceProvider();
            desktop.MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            desktop.Exit += (_, _) =>
            {
                _serviceProvider?.Dispose();
                _serviceProvider = null;
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServiceProvider()
    {
        ServiceCollection services = new();
        ConfigureServices(services);
        return services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.TryAddSingleton(CreateApiHttpClient());
        services.TryAddSingleton<IChummerClient, HttpChummerClient>();
        services.TryAddSingleton<ICharacterOverviewPresenter, CharacterOverviewPresenter>();
        services.TryAddSingleton<IShellPresenter, ShellPresenter>();
        services.TryAddSingleton<ICommandAvailabilityEvaluator, DefaultCommandAvailabilityEvaluator>();
        services.TryAddSingleton<CharacterOverviewViewModelAdapter>();
        services.TryAddSingleton<MainWindow>();
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
