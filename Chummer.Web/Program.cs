using Chummer.Application.Characters;
using Chummer.Application.Tools;
using Chummer.Application.Workspaces;
using Chummer.Core.Characters;
using Chummer.Core.LifeModules;
using Chummer.Infrastructure.Files;
using Chummer.Infrastructure.Workspaces;
using Chummer.Infrastructure.Xml;
using Chummer.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICharacterFileService, CharacterFileService>();
builder.Services.AddSingleton<ICharacterSectionService, CharacterSectionService>();
builder.Services.AddSingleton<ICharacterFileQueries, XmlCharacterFileQueries>();
builder.Services.AddSingleton<ICharacterMetadataCommands, XmlCharacterMetadataCommands>();
builder.Services.AddSingleton<ICharacterOverviewQueries, XmlCharacterOverviewQueries>();
builder.Services.AddSingleton<ICharacterStatsQueries, XmlCharacterStatsQueries>();
builder.Services.AddSingleton<ICharacterInventoryQueries, XmlCharacterInventoryQueries>();
builder.Services.AddSingleton<ICharacterMagicResonanceQueries, XmlCharacterMagicResonanceQueries>();
builder.Services.AddSingleton<ICharacterSocialNarrativeQueries, XmlCharacterSocialNarrativeQueries>();
builder.Services.AddSingleton<ICharacterSectionQueries>(provider =>
    new XmlCharacterSectionQueries(
        provider.GetRequiredService<ICharacterOverviewQueries>(),
        provider.GetRequiredService<ICharacterStatsQueries>(),
        provider.GetRequiredService<ICharacterInventoryQueries>(),
        provider.GetRequiredService<ICharacterMagicResonanceQueries>(),
        provider.GetRequiredService<ICharacterSocialNarrativeQueries>()));
builder.Services.AddSingleton<ILifeModulesService>(_ =>
{
    string path = LifeModulesPathResolver.Resolve(AppContext.BaseDirectory, Directory.GetCurrentDirectory());
    return new LifeModulesService(path);
});
builder.Services.AddSingleton<IDataExportService, DataExportService>();
builder.Services.AddSingleton<IToolCatalogService, XmlToolCatalogService>();
builder.Services.AddSingleton<ISettingsStore, FileSettingsStore>();
builder.Services.AddSingleton<IRosterStore, FileRosterStore>();
builder.Services.AddSingleton<IWorkspaceStore, InMemoryWorkspaceStore>();
builder.Services.AddSingleton<IWorkspaceService, WorkspaceService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapInfoEndpoints();
app.MapCharacterEndpoints();
app.MapLifeModulesEndpoints();
app.MapToolsEndpoints();
app.MapSettingsEndpoints();
app.MapRosterEndpoints();
app.MapCommandEndpoints();
app.MapWorkspaceEndpoints();

app.Run();
