using Chummer.Application.Tools;
using Chummer.Core.Characters;
using Chummer.Core.LifeModules;
using Chummer.Infrastructure.Files;
using Chummer.Infrastructure.Xml;
using Chummer.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICharacterFileService, CharacterFileService>();
builder.Services.AddSingleton<ICharacterSectionService, CharacterSectionService>();
builder.Services.AddSingleton<ILifeModulesService>(_ =>
{
    string path = LifeModulesPathResolver.Resolve(AppContext.BaseDirectory, Directory.GetCurrentDirectory());
    return new LifeModulesService(path);
});
builder.Services.AddSingleton<IDataExportService, DataExportService>();
builder.Services.AddSingleton<ISettingsStore, FileSettingsStore>();
builder.Services.AddSingleton<IRosterStore, FileRosterStore>();

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

app.Run();
