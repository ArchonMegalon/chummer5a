using Chummer.Infrastructure.DependencyInjection;
using Chummer.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddChummerHeadlessCore(AppContext.BaseDirectory, Directory.GetCurrentDirectory());

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
