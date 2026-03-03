using Chummer.Application.Characters;
using Chummer.Application.LifeModules;
using Chummer.Application.Tools;
using Chummer.Application.Workspaces;
using Chummer.Core.Characters;
using Chummer.Infrastructure.Files;
using Chummer.Infrastructure.Workspaces;
using Chummer.Infrastructure.Xml;
using Microsoft.Extensions.DependencyInjection;

namespace Chummer.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChummerHeadlessCore(
        this IServiceCollection services,
        string baseDirectory,
        string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);

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

        services.AddSingleton<ILifeModulesCatalogService>(_ =>
        {
            string path = LifeModulesCatalogPathResolver.Resolve(baseDirectory, currentDirectory);
            return new XmlLifeModulesCatalogService(path);
        });

        services.AddSingleton<IDataExportService, DataExportService>();
        services.AddSingleton<IToolCatalogService, XmlToolCatalogService>();
        services.AddSingleton<ISettingsStore, FileSettingsStore>();
        services.AddSingleton<IRosterStore, FileRosterStore>();
        services.AddSingleton<IWorkspaceStore, InMemoryWorkspaceStore>();
        services.AddSingleton<IWorkspaceService, WorkspaceService>();

        return services;
    }
}
