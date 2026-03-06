using Chummer.Application.Characters;
using Chummer.Application.Content;
using Chummer.Application.Hub;
using Chummer.Application.Owners;
using Chummer.Application.LifeModules;
using Chummer.Application.Session;
using Chummer.Application.Tools;
using Chummer.Application.Workspaces;
using Chummer.Infrastructure.Files;
using Chummer.Infrastructure.Owners;
using Chummer.Infrastructure.Workspaces;
using Chummer.Infrastructure.Xml;
using Chummer.Rulesets.Hosting;
using Chummer.Rulesets.Sr5;
using Chummer.Rulesets.Sr6;
using Microsoft.Extensions.DependencyInjection;

namespace Chummer.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    private const string StatePathEnvironmentVariable = "CHUMMER_STATE_PATH";
    private const string WorkspaceStorePathEnvironmentVariable = "CHUMMER_WORKSPACE_STORE_PATH";
    private const string AmendsPathEnvironmentVariable = "CHUMMER_AMENDS_PATH";
    private const string RequireContentBundleEnvironmentVariable = "CHUMMER_REQUIRE_CONTENT_BUNDLE";

    public static IServiceCollection AddChummerHeadlessCore(
        this IServiceCollection services,
        string baseDirectory,
        string currentDirectory,
        bool requireContentBundle = false)
    {
        ArgumentNullException.ThrowIfNull(services);
        string stateDirectory = ResolveStateDirectory(baseDirectory);
        string? amendsDirectory = Environment.GetEnvironmentVariable(AmendsPathEnvironmentVariable);
        bool validateContentBundle = requireContentBundle || ResolveBooleanEnvironmentVariable(RequireContentBundleEnvironmentVariable);
        var overlays = new FileSystemContentOverlayCatalogService(baseDirectory, currentDirectory, amendsDirectory);
        if (validateContentBundle)
        {
            ValidateContentBundle(overlays);
        }

        services.AddSingleton<ICharacterFileService, CharacterFileService>();
        services.AddRulesetInfrastructure();
        services.AddSr5Ruleset();
        services.AddSr6Ruleset();
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
        services.AddSingleton<IContentOverlayCatalogService>(overlays);
        services.AddSingleton<IBuildKitRegistryService, DefaultBuildKitRegistryService>();
        services.AddSingleton<IRulePackRegistryService, OverlayRulePackRegistryService>();
        services.AddSingleton<IRuntimeFingerprintService, DefaultRuntimeFingerprintService>();
        services.AddSingleton<IRuleProfileRegistryService, DefaultRuleProfileRegistryService>();
        services.AddSingleton<IRuleProfileApplicationService, DefaultRuleProfileApplicationService>();
        services.AddSingleton<IRuntimeInspectorService, DefaultRuntimeInspectorService>();
        services.AddSingleton<IRuntimeLockRegistryService, ProfileBackedRuntimeLockRegistryService>();
        services.AddSingleton<IHubCatalogService, DefaultHubCatalogService>();
        services.AddSingleton<IHubInstallPreviewService, DefaultHubInstallPreviewService>();
        services.AddSingleton<IHubProjectCompatibilityService, DefaultHubProjectCompatibilityService>();

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
        services.AddSingleton<IOwnerContextAccessor, LocalOwnerContextAccessor>();
        services.AddSingleton<IShellPreferencesStore, SettingsShellPreferencesStore>();
        services.AddSingleton<IShellPreferencesService, ShellPreferencesService>();
        services.AddSingleton<IShellSessionStore, SettingsShellSessionStore>();
        services.AddSingleton<IShellSessionService, ShellSessionService>();
        services.AddSingleton<ISessionService, NotImplementedSessionService>();
        services.AddSingleton<IRosterStore>(_ => new FileRosterStore(stateDirectory));
        services.AddSingleton<IWorkspaceStore>(_ =>
        {
            string? workspaceDirectory = Environment.GetEnvironmentVariable(WorkspaceStorePathEnvironmentVariable);
            return string.IsNullOrWhiteSpace(workspaceDirectory)
                ? new FileWorkspaceStore(stateDirectory)
                : new FileWorkspaceStore(workspaceDirectory);
        });
        services.AddSingleton<IWorkspaceImportRulesetDetector, WorkspaceImportRulesetDetector>();
        services.AddSingleton<IWorkspaceService, WorkspaceService>();

        return services;
    }

    private static void ValidateContentBundle(IContentOverlayCatalogService overlays)
    {
        ArgumentNullException.ThrowIfNull(overlays);

        IReadOnlyList<string> dataDirectories = overlays.GetDataDirectories();
        if (dataDirectories.Count == 0)
        {
            throw new InvalidOperationException(
                "Content bundle validation failed: no data directories were discovered. " +
                "Set CHUMMER_AMENDS_PATH correctly or include bundled /data content.");
        }

        IReadOnlyList<string> languageDirectories = overlays.GetLanguageDirectories();
        if (languageDirectories.Count == 0)
        {
            throw new InvalidOperationException(
                "Content bundle validation failed: no language directories were discovered. " +
                "Set CHUMMER_AMENDS_PATH correctly or include bundled /lang content.");
        }

        try
        {
            overlays.ResolveDataFile("lifemodules.xml");
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException)
        {
            throw new InvalidOperationException(
                "Content bundle validation failed: required data file 'lifemodules.xml' is missing from effective content paths.",
                ex);
        }

        bool hasAnyLanguageXml = languageDirectories
            .Any(directory => Directory.Exists(directory)
                && Directory.EnumerateFiles(directory, "*.xml", SearchOption.TopDirectoryOnly).Any());
        if (!hasAnyLanguageXml)
        {
            throw new InvalidOperationException(
                "Content bundle validation failed: no language XML files were discovered in effective language paths.");
        }
    }

    private static string ResolveStateDirectory(string baseDirectory)
    {
        string? configured = Environment.GetEnvironmentVariable(StatePathEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        return Path.Combine(baseDirectory, "state");
    }

    private static bool ResolveBooleanEnvironmentVariable(string variableName)
    {
        string? raw = Environment.GetEnvironmentVariable(variableName);
        return bool.TryParse(raw, out bool parsed) && parsed;
    }
}
