using Chummer.Infrastructure.DependencyInjection;
using Chummer.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddChummerHeadlessCore(AppContext.BaseDirectory, Directory.GetCurrentDirectory());

var app = builder.Build();

app.MapInfoEndpoints();
app.MapCharacterEndpoints();
app.MapLifeModulesEndpoints();
app.MapToolsEndpoints();
app.MapSettingsEndpoints();
app.MapRosterEndpoints();
app.MapCommandEndpoints();
app.MapWorkspaceEndpoints();

app.Run();
