using System.Security.Cryptography;
using System.Text;
using Chummer.Application.Owners;
using Chummer.Infrastructure.DependencyInjection;
using Chummer.Api.Endpoints;
using Chummer.Api.Owners;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddChummerHeadlessCore(AppContext.BaseDirectory, Directory.GetCurrentDirectory(), requireContentBundle: true);
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

bool allowOwnerHeader = ResolveBoolean(
    builder.Configuration["Chummer:AllowOwnerHeader"],
    Environment.GetEnvironmentVariable("CHUMMER_ALLOW_OWNER_HEADER"));
string ownerHeaderName = ResolveOwnerHeaderName(
    builder.Configuration["Chummer:OwnerHeaderName"],
    Environment.GetEnvironmentVariable("CHUMMER_OWNER_HEADER_NAME"));
builder.Services.AddSingleton<IOwnerContextAccessor>(provider =>
    new RequestOwnerContextAccessor(
        provider.GetRequiredService<IHttpContextAccessor>(),
        allowOwnerHeader ? ownerHeaderName : null));

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

string? configuredApiKey = builder.Configuration["Chummer:ApiKey"];
string? environmentApiKey = Environment.GetEnvironmentVariable("CHUMMER_API_KEY");
string? apiKey = configuredApiKey ?? environmentApiKey;
bool protectApiDocs = ResolveBoolean(
    builder.Configuration["Chummer:ProtectApiDocs"],
    Environment.GetEnvironmentVariable("CHUMMER_PROTECT_API_DOCS"));

if (!string.IsNullOrWhiteSpace(apiKey))
{
    app.Use(async (context, next) =>
    {
        PathString path = context.Request.Path;
        if (!RequiresApiKey(path, protectApiDocs))
        {
            await next();
            return;
        }

        if (path.StartsWithSegments("/api", StringComparison.Ordinal)
            && (IsPublicApiPath(path) || HttpMethods.IsOptions(context.Request.Method)))
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
app.MapShellEndpoints();
app.MapWorkspaceEndpoints();

app.Run();

static bool IsPublicApiPath(PathString path)
{
    return path.StartsWithSegments("/api/health", StringComparison.Ordinal)
        || path.StartsWithSegments("/api/info", StringComparison.Ordinal)
        || path.StartsWithSegments("/api/content/overlays", StringComparison.Ordinal)
        || path.StartsWithSegments("/api/commands", StringComparison.Ordinal)
        || path.StartsWithSegments("/api/navigation-tabs", StringComparison.Ordinal)
        || path.StartsWithSegments("/api/shell/bootstrap", StringComparison.Ordinal);
}

static bool RequiresApiKey(PathString path, bool protectApiDocs)
{
    if (path.StartsWithSegments("/api", StringComparison.Ordinal))
    {
        return true;
    }

    if (!protectApiDocs)
    {
        return false;
    }

    return path.StartsWithSegments("/openapi", StringComparison.Ordinal)
        || path.StartsWithSegments("/docs", StringComparison.Ordinal);
}

static bool ResolveBoolean(string? configuredValue, string? environmentValue)
{
    string? raw = configuredValue ?? environmentValue;
    return bool.TryParse(raw, out bool parsed) && parsed;
}

static string ResolveOwnerHeaderName(string? configuredValue, string? environmentValue)
{
    string? raw = configuredValue ?? environmentValue;
    return string.IsNullOrWhiteSpace(raw)
        ? "X-Chummer-Owner"
        : raw.Trim();
}

static bool ConstantTimeEquals(string left, string right)
{
    byte[] leftBytes = Encoding.UTF8.GetBytes(left);
    byte[] rightBytes = Encoding.UTF8.GetBytes(right);
    if (leftBytes.Length != rightBytes.Length)
        return false;

    return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
}
