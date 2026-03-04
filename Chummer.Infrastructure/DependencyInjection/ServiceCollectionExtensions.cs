using Chummer.Application.Characters;
using Chummer.Application.Content;
using Chummer.Application.LifeModules;
using Chummer.Application.Tools;
using Chummer.Application.Workspaces;
using Chummer.Infrastructure.Files;
using Chummer.Infrastructure.Workspaces;
using Chummer.Infrastructure.Xml;
using Microsoft.Extensions.DependencyInjection;

namespace Chummer.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    private const string StatePathEnvironmentVariable = "CHUMMER_STATE_PATH";
    private const string WorkspaceStorePathEnvironmentVariable = "CHUMMER_WORKSPACE_STORE_PATH";
    private const string AmendsPathEnvironmentVariable = "CHUMMER_AMENDS_PATH";

    public static IServiceCollection AddChummerHeadlessCore(
        this IServiceCollection services,
        string baseDirectory,
        string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);
        string stateDirectory = ResolveStateDirectory(baseDirectory);
        string? amendsDirectory = Environment.GetEnvironmentVariable(AmendsPathEnvironmentVariable);

        services.AddSingleton<ICharacterFileService, CharacterFileService>();
        services.AddSingleton<ICharacterSectionService, CharacterSectionService>();
        services.AddSingleton<ICharacterFileQueries, XmlCharacterFileQueries>();
        services.AddSingleton<ICharacterMetadataCommands, XmlCharacterMetadataCommands>();
        services.AddSingleton<ICharacterOverviewQueries, XmlCharacterOverviewQueries>();
        services.AddSingleton<ICharacterStatsQueries, XmlCharacterStatsQueries>();
        services.AddSingleton<ICharacterInventoryQueries, XmlCharacterInventoryQueries>();
        services.AddSingleton<ICharacterMagicResonanceQueries, XmlCharacterMagicResonanceQueries>();
        services.AddSingleton<ICharacterSocialNarrativeQueries, XmlCharacterSocialNarrativeQueries>();
        services.AddSingleton<ICharacterSectionQueries>(provider =>
            new XmlCharacterSectionQueries(
                provider.GetRequiredService<ICharacterOverviewQueries>(),
                provider.GetRequiredService<ICharacterStatsQueries>(),
                provider.GetRequiredService<ICharacterInventoryQueries>(),
                provider.GetRequiredService<ICharacterMagicResonanceQueries>(),
                provider.GetRequiredService<ICharacterSocialNarrativeQueries>()));
        services.AddSingleton<IContentOverlayCatalogService>(_ =>
            new FileSystemContentOverlayCatalogService(baseDirectory, currentDirectory, amendsDirectory));

        services.AddSingleton<ILifeModulesCatalogService>(provider =>
        {
            var overlays = provider.GetRequiredService<IContentOverlayCatalogService>();
            string path = LifeModulesCatalogPathResolver.Resolve(overlays);
            return new XmlLifeModulesCatalogService(path);
        });

        services.AddSingleton<IDataExportService, DataExportService>();
        services.AddSingleton<IToolCatalogService>(provider =>
            new XmlToolCatalogService(provider.GetRequiredService<IContentOverlayCatalogService>()));
        services.AddSingleton<ISettingsStore>(_ => new FileSettingsStore(stateDirectory));
        services.AddSingleton<IRosterStore>(_ => new FileRosterStore(stateDirectory));
        services.AddSingleton<IWorkspaceStore>(_ =>
        {
            string? workspaceDirectory = Environment.GetEnvironmentVariable(WorkspaceStorePathEnvironmentVariable);
            return string.IsNullOrWhiteSpace(workspaceDirectory)
                ? new FileWorkspaceStore(stateDirectory)
                : new FileWorkspaceStore(workspaceDirectory);
        });
        services.AddSingleton<IWorkspaceService, WorkspaceService>();

        return services;
    }

    private static string ResolveStateDirectory(string baseDirectory)
    {
        string? configured = Environment.GetEnvironmentVariable(StatePathEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        return Path.Combine(baseDirectory, "state");
    }
}
