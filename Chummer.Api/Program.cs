using System.Security.Cryptography;
using System.Text;
using Chummer.Application.Owners;
using Chummer.Contracts.Owners;
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
string portalOwnerSharedKey = ResolvePortalOwnerSharedKey(
    builder.Configuration["Chummer:PortalOwnerSharedKey"],
    Environment.GetEnvironmentVariable(PortalOwnerPropagationContract.SharedKeyEnvironmentVariable));
TimeSpan portalOwnerMaxAge = ResolvePortalOwnerMaxAge(
    builder.Configuration["Chummer:PortalOwnerMaxAgeSeconds"],
    Environment.GetEnvironmentVariable("CHUMMER_PORTAL_OWNER_MAX_AGE_SECONDS"));
builder.Services.AddSingleton<IOwnerContextAccessor>(provider =>
    new RequestOwnerContextAccessor(
        provider.GetRequiredService<IHttpContextAccessor>(),
        allowOwnerHeader ? ownerHeaderName : null,
        portalOwnerSharedKey,
        portalOwnerMaxAge));

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

string? configuredApiKey = builder.Configuration["Chummer:ApiKey"];
string? environmentApiKey = Environment.GetEnvironmentVariable("CHUMMER_API_KEY");
string? apiKey = configuredApiKey ?? environmentApiKey;
bool isPortalOwnerPropagationConfigured = !string.IsNullOrWhiteSpace(portalOwnerSharedKey);
bool protectApiDocs = ResolveBoolean(
    builder.Configuration["Chummer:ProtectApiDocs"],
    Environment.GetEnvironmentVariable("CHUMMER_PROTECT_API_DOCS"));

if (!string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("INFO: CHUMMER_API_KEY direct API protection is enabled. Treat X-Api-Key mode as local/dev/ops or private-upstream protection, not as the primary public authentication surface.");
    app.Use(async (context, next) =>
    {
        PathString path = context.Request.Path;
        if (!RequiresApiKey(path, protectApiDocs))
        {
            await next();
            return;
        }

        if (path.StartsWithSegments("/api", StringComparison.Ordinal)
            && (HttpMethods.IsOptions(context.Request.Method) || AllowsPublicApiKeyBypass(context)))
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
else if (app.Environment.IsProduction() && isPortalOwnerPropagationConfigured)
{
    Console.WriteLine("INFO: CHUMMER_API_KEY is not configured. Hosted/public deployments should expose Chummer.Portal as the public edge and keep Chummer.Api private behind signed portal-owner propagation.");
}
else if (app.Environment.IsProduction())
{
    Console.WriteLine("WARNING: Neither CHUMMER_API_KEY nor CHUMMER_PORTAL_OWNER_SHARED_KEY is configured. Public deployments should expose Chummer.Portal as the public edge and keep Chummer.Api private.");
}

app.MapOpenApi("/openapi/{documentName}.json");
app.MapGet("/docs", () => Results.Redirect("/docs/index.html"));

app.MapInfoEndpoints();
app.MapHubCatalogEndpoints();
app.MapHubPublisherEndpoints();
app.MapHubReviewEndpoints();
app.MapHubPublicationEndpoints();
app.MapBuildKitRegistryEndpoints();
app.MapRulePackRegistryEndpoints();
app.MapRuleProfileRegistryEndpoints();
app.MapRuntimeInspectorEndpoints();
app.MapRuntimeLockRegistryEndpoints();
app.MapCharacterEndpoints();
app.MapLifeModulesEndpoints();
app.MapToolsEndpoints();
app.MapSettingsEndpoints();
app.MapRosterEndpoints();
app.MapCommandEndpoints();
app.MapNavigationEndpoints();
app.MapShellEndpoints();
app.MapSessionEndpoints();
app.MapWorkspaceEndpoints();

app.Run();

static bool AllowsPublicApiKeyBypass(HttpContext context)
{
    return context.GetEndpoint()?.Metadata.GetMetadata<PublicApiEndpointMetadata>() is not null;
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

static string ResolvePortalOwnerSharedKey(string? configuredValue, string? environmentValue)
{
    string? raw = configuredValue ?? environmentValue;
    return string.IsNullOrWhiteSpace(raw)
        ? string.Empty
        : raw.Trim();
}

static TimeSpan ResolvePortalOwnerMaxAge(string? configuredValue, string? environmentValue)
{
    string? raw = configuredValue ?? environmentValue;
    if (!int.TryParse(raw, out int seconds) || seconds <= 0)
    {
        seconds = PortalOwnerPropagationContract.DefaultMaxAgeSeconds;
    }

    return TimeSpan.FromSeconds(seconds);
}

static bool ConstantTimeEquals(string left, string right)
{
    byte[] leftBytes = Encoding.UTF8.GetBytes(left);
    byte[] rightBytes = Encoding.UTF8.GetBytes(right);
    if (leftBytes.Length != rightBytes.Length)
        return false;

    return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
}
