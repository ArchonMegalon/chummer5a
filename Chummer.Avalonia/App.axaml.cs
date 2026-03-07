using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Chummer.Application.AI;
using Chummer.Desktop.Runtime;
using Chummer.Contracts.Presentation;
using Chummer.Presentation;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Chummer.Avalonia;

public partial class App : global::Avalonia.Application
{
    private const string ClientModeEnvironmentVariable = "CHUMMER_CLIENT_MODE";
    private const string LegacyDesktopClientModeEnvironmentVariable = "CHUMMER_DESKTOP_CLIENT_MODE";

    private ServiceProvider? _serviceProvider;
    internal static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _serviceProvider = BuildServiceProvider();
            Services = _serviceProvider;
            desktop.MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            desktop.Exit += (_, _) =>
            {
                Services = null;
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
        services.AddChummerLocalRuntimeClient(AppContext.BaseDirectory, Directory.GetCurrentDirectory());
        services.AddSingleton<IAvaloniaCoachSidecarClient>(serviceProvider =>
            UseHttpClientMode()
                ? new HttpAvaloniaCoachSidecarClient(serviceProvider.GetRequiredService<HttpClient>())
                : new InProcessAvaloniaCoachSidecarClient(serviceProvider.GetRequiredService<IAiGatewayService>()));
        services.AddSingleton<IShellBootstrapDataProvider, ShellBootstrapDataProvider>();
        services.AddSingleton<ICharacterOverviewPresenter, CharacterOverviewPresenter>();
        services.AddSingleton<IShellPresenter, ShellPresenter>();
        services.AddSingleton<ICommandAvailabilityEvaluator, DefaultCommandAvailabilityEvaluator>();
        services.AddSingleton<IShellSurfaceResolver, ShellSurfaceResolver>();
        services.AddSingleton<CharacterOverviewViewModelAdapter>();
        services.AddSingleton<MainWindow>();
    }

    private static bool UseHttpClientMode()
    {
        string? raw = Environment.GetEnvironmentVariable(ClientModeEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(raw))
        {
            raw = Environment.GetEnvironmentVariable(LegacyDesktopClientModeEnvironmentVariable);
        }

        return string.Equals(raw?.Trim(), "http", StringComparison.OrdinalIgnoreCase);
    }
}
