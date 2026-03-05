using Chummer.Presentation;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;
using Chummer.Desktop.Runtime;
using Chummer.Contracts.Rulesets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Photino.Blazor;

namespace Chummer.Blazor.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        appBuilder.Services.AddLogging();
        appBuilder.Services.AddChummerLocalRuntimeClient(AppContext.BaseDirectory, Directory.GetCurrentDirectory());
        appBuilder.Services.AddSingleton<IShellBootstrapDataProvider, ShellBootstrapDataProvider>();
        appBuilder.Services.AddSingleton<ICharacterOverviewPresenter, CharacterOverviewPresenter>();
        appBuilder.Services.AddSingleton<IShellPresenter, ShellPresenter>();
        appBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IRulesetPlugin, Sr5RulesetPlugin>());
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
}
