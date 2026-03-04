using System.Security.Cryptography;
using System.Text;
using Chummer.Infrastructure.DependencyInjection;
using Chummer.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddChummerHeadlessCore(AppContext.BaseDirectory, Directory.GetCurrentDirectory());
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

string? configuredApiKey = builder.Configuration["Chummer:ApiKey"];
string? environmentApiKey = Environment.GetEnvironmentVariable("CHUMMER_API_KEY");
string? apiKey = configuredApiKey ?? environmentApiKey;

if (!string.IsNullOrWhiteSpace(apiKey))
{
    app.Use(async (context, next) =>
    {
        PathString path = context.Request.Path;
        if (!path.StartsWithSegments("/api", StringComparison.Ordinal))
        {
            await next();
            return;
        }

        if (IsPublicApiPath(path) || HttpMethods.IsOptions(context.Request.Method))
        {
            await next();
            return;
        }

        string? provided = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(provided) || !ConstantTimeEquals(provided, apiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "missing_or_invalid_api_key",
                header = "X-Api-Key"
            });
            return;
        }

        await next();
    });
}
else if (app.Environment.IsProduction())
{
    Console.WriteLine("WARNING: CHUMMER_API_KEY is not configured; API key middleware is disabled.");
}

app.MapOpenApi("/openapi/{documentName}.json");
app.MapGet("/docs", () => Results.Redirect("/docs/index.html"));

app.MapInfoEndpoints();
app.MapCharacterEndpoints();
app.MapLifeModulesEndpoints();
app.MapToolsEndpoints();
app.MapSettingsEndpoints();
app.MapRosterEndpoints();
app.MapCommandEndpoints();
app.MapNavigationEndpoints();
app.MapWorkspaceEndpoints();

app.Run();

static bool IsPublicApiPath(PathString path)
{
    return path.StartsWithSegments("/api/health", StringComparison.Ordinal)
        || path.StartsWithSegments("/api/info", StringComparison.Ordinal)
        || path.StartsWithSegments("/api/content/overlays", StringComparison.Ordinal)
        || path.StartsWithSegments("/api/commands", StringComparison.Ordinal)
        || path.StartsWithSegments("/api/navigation-tabs", StringComparison.Ordinal);
}

static bool ConstantTimeEquals(string left, string right)
{
    byte[] leftBytes = Encoding.UTF8.GetBytes(left);
    byte[] rightBytes = Encoding.UTF8.GetBytes(right);
    if (leftBytes.Length != rightBytes.Length)
        return false;

    return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
}
