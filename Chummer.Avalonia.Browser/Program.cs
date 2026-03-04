var builder = WebApplication.CreateBuilder(args);
string? configuredPathBase = builder.Configuration["AvaloniaBrowser:PathBase"];
string? environmentPathBase = Environment.GetEnvironmentVariable("CHUMMER_AVALONIA_BROWSER_PATH_BASE");
PathString pathBase = NormalizePathBase(configuredPathBase ?? environmentPathBase);

var app = builder.Build();

if (pathBase.HasValue)
{
    app.UsePathBase(pathBase);
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(BuildHealthPayload(pathBase)));
app.MapGet("/avalonia/health", () => Results.Ok(BuildHealthPayload(pathBase)));

app.MapFallbackToFile("index.html");
app.Run();

static PathString NormalizePathBase(string? rawPathBase)
{
    if (string.IsNullOrWhiteSpace(rawPathBase))
    {
        return PathString.Empty;
    }

    string normalized = rawPathBase.Trim();
    if (!normalized.StartsWith("/", StringComparison.Ordinal))
    {
        normalized = "/" + normalized;
    }

    if (normalized.Length > 1 && normalized.EndsWith("/", StringComparison.Ordinal))
    {
        normalized = normalized.TrimEnd('/');
    }

    return normalized == "/" ? PathString.Empty : new PathString(normalized);
}

static object BuildHealthPayload(PathString pathBase)
{
    return new
    {
        ok = true,
        head = "avalonia-browser",
        pathBase = pathBase.Value,
        utc = DateTimeOffset.UtcNow
    };
}
