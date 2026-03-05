using System.Text.Json;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

string blazorBaseUrl = PortalSettingsResolver.ResolveSetting(builder.Configuration, "Portal:BlazorBaseUrl", "CHUMMER_PORTAL_BLAZOR_URL", "http://127.0.0.1:8089/");
string blazorProxyBaseUrl = PortalSettingsResolver.ResolveSetting(builder.Configuration, "Portal:BlazorProxyBaseUrl", "CHUMMER_PORTAL_BLAZOR_PROXY_URL", string.Empty);
string avaloniaBrowserBaseUrl = PortalSettingsResolver.ResolveSetting(builder.Configuration, "Portal:AvaloniaBrowserBaseUrl", "CHUMMER_PORTAL_AVALONIA_URL", "/avalonia/");
string avaloniaProxyBaseUrl = PortalSettingsResolver.ResolveSetting(builder.Configuration, "Portal:AvaloniaProxyBaseUrl", "CHUMMER_PORTAL_AVALONIA_PROXY_URL", string.Empty);
string apiBaseUrl = PortalSettingsResolver.ResolveSetting(builder.Configuration, "Portal:ApiBaseUrl", "CHUMMER_PORTAL_API_URL", "http://chummer-api:8080/");
string apiProxyKey = PortalSettingsResolver.ResolveSetting(builder.Configuration, "Portal:ApiKey", "CHUMMER_PORTAL_API_KEY", Environment.GetEnvironmentVariable("CHUMMER_API_KEY") ?? string.Empty);
string downloadsBaseUrl = PortalSettingsResolver.ResolveSetting(builder.Configuration, "Portal:DownloadsBaseUrl", "CHUMMER_PORTAL_DOWNLOADS_URL", "/downloads/");
string downloadsProxyBaseUrl = PortalSettingsResolver.ResolveSetting(builder.Configuration, "Portal:DownloadsProxyBaseUrl", "CHUMMER_PORTAL_DOWNLOADS_PROXY_URL", string.Empty);
string downloadsFallbackUrl = PortalSettingsResolver.ResolveSetting(builder.Configuration, "Portal:DownloadsFallbackUrl", "CHUMMER_PORTAL_DOWNLOADS_FALLBACK_URL", string.Empty);
string releaseManifestPath = PortalSettingsResolver.ResolveSetting(builder.Configuration, "Portal:ReleaseManifestPath", "CHUMMER_PORTAL_RELEASES_FILE", "downloads/releases.json");
string releaseFilesPath = PortalSettingsResolver.ResolveSetting(builder.Configuration, "Portal:ReleaseFilesPath", "CHUMMER_PORTAL_RELEASES_DIR", string.Empty);
string resolvedManifestPath = PortalDownloadsService.ResolveManifestPath(releaseManifestPath);
string resolvedReleaseFilesPath = PortalDownloadsService.ResolveReleaseFilesPath(releaseFilesPath, resolvedManifestPath);
bool useBlazorProxy = !string.IsNullOrWhiteSpace(blazorProxyBaseUrl);
bool useAvaloniaProxy = !string.IsNullOrWhiteSpace(avaloniaProxyBaseUrl);
bool useDownloadsProxy = !string.IsNullOrWhiteSpace(downloadsProxyBaseUrl);
bool isApiKeyForwardingEnabled = !string.IsNullOrWhiteSpace(apiProxyKey);
IReadOnlyList<IReadOnlyDictionary<string, string>>? apiRouteTransforms = PortalProxyUtils.BuildApiRouteTransforms(apiProxyKey);

var proxyRoutes = new List<RouteConfig>
{
    new RouteConfig
    {
        RouteId = "portal-api",
        ClusterId = "api-cluster",
        Match = new RouteMatch
        {
            Path = "/api/{**catch-all}"
        },
        Transforms = PortalProxyUtils.BuildRouteTransforms(apiRouteTransforms)
    },
    new RouteConfig
    {
        RouteId = "portal-docs",
        ClusterId = "api-cluster",
        Match = new RouteMatch
        {
            Path = "/docs/{**catch-all}"
        },
        Transforms = PortalProxyUtils.BuildRouteTransforms(apiRouteTransforms)
    },
    new RouteConfig
    {
        RouteId = "portal-openapi",
        ClusterId = "api-cluster",
        Match = new RouteMatch
        {
            Path = "/openapi/{**catch-all}"
        },
        Transforms = PortalProxyUtils.BuildRouteTransforms(apiRouteTransforms)
    }
};

var proxyClusters = new List<ClusterConfig>
{
    new ClusterConfig
    {
        ClusterId = "api-cluster",
        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.Ordinal)
        {
            ["primary"] = new DestinationConfig
            {
                Address = PortalProxyUtils.NormalizeProxyAddress(apiBaseUrl)
            }
        }
    }
};

if (useBlazorProxy)
{
    proxyRoutes.Add(new RouteConfig
    {
        RouteId = "portal-blazor",
        ClusterId = "blazor-cluster",
        Match = new RouteMatch
        {
            Path = "/blazor/{**catch-all}"
        }
    });

    proxyClusters.Add(new ClusterConfig
    {
        ClusterId = "blazor-cluster",
        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.Ordinal)
        {
            ["primary"] = new DestinationConfig
            {
                Address = PortalProxyUtils.NormalizeProxyAddress(blazorProxyBaseUrl)
            }
        }
    });
}

if (useAvaloniaProxy)
{
    proxyRoutes.Add(new RouteConfig
    {
        RouteId = "portal-avalonia",
        ClusterId = "avalonia-cluster",
        Match = new RouteMatch
        {
            Path = "/avalonia/{**catch-all}"
        }
    });

    proxyClusters.Add(new ClusterConfig
    {
        ClusterId = "avalonia-cluster",
        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.Ordinal)
        {
            ["primary"] = new DestinationConfig
            {
                Address = PortalProxyUtils.NormalizeProxyAddress(avaloniaProxyBaseUrl)
            }
        }
    });
}

if (useDownloadsProxy)
{
    proxyRoutes.Add(new RouteConfig
    {
        RouteId = "portal-downloads",
        ClusterId = "downloads-cluster",
        Match = new RouteMatch
        {
            Path = "/downloads/{**catch-all}"
        }
    });

    proxyClusters.Add(new ClusterConfig
    {
        ClusterId = "downloads-cluster",
        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.Ordinal)
        {
            ["primary"] = new DestinationConfig
            {
                Address = PortalProxyUtils.NormalizeProxyAddress(downloadsProxyBaseUrl)
            }
        }
    });
}

builder.Services.AddReverseProxy()
    .LoadFromMemory(proxyRoutes, proxyClusters);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    ok = true,
    head = "portal",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/", () => Results.Content(
    PortalPageBuilder.BuildLandingHtml(
        blazorBaseUrl,
        blazorProxyBaseUrl,
        useBlazorProxy,
        avaloniaBrowserBaseUrl,
        avaloniaProxyBaseUrl,
        useAvaloniaProxy,
        apiBaseUrl,
        isApiKeyForwardingEnabled,
        downloadsBaseUrl,
        downloadsProxyBaseUrl,
        useDownloadsProxy),
    "text/html; charset=utf-8"));

app.MapGet("/downloads/releases.json", () => Results.Json(
    PortalDownloadsService.LoadReleaseManifest(resolvedManifestPath, resolvedReleaseFilesPath, downloadsFallbackUrl),
    new JsonSerializerOptions(JsonSerializerDefaults.Web)));

app.MapGet("/downloads/", () => Results.Content(
    PortalPageBuilder.BuildDownloadsHtml(downloadsFallbackUrl, PortalDownloadsService.HasConfiguredFallbackSource(downloadsFallbackUrl)),
    "text/html; charset=utf-8"));

if (!useBlazorProxy)
{
    app.MapGet("/blazor/{**path}", (HttpContext context, string? path) =>
        Results.Redirect(PortalProxyUtils.ComposeRedirect(blazorBaseUrl, path, context.Request.QueryString)));
}

if (!useAvaloniaProxy)
{
    string avaloniaDownloadsTarget = PortalDownloadsService.HasConfiguredFallbackSource(downloadsFallbackUrl)
        ? downloadsFallbackUrl
        : downloadsBaseUrl;
    app.MapGet("/avalonia/{**path}", () => Results.Content(
        PortalPageBuilder.BuildAvaloniaPlaceholderHtml(avaloniaBrowserBaseUrl, avaloniaDownloadsTarget),
        "text/html; charset=utf-8"));
}

if (!useDownloadsProxy)
{
    app.MapGet("/downloads/{**path}", (HttpContext context, string? path) =>
    {
        string? filePath = PortalDownloadsService.ResolveDownloadFilePath(resolvedReleaseFilesPath, path);
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            string fileName = Path.GetFileName(filePath);
            return Results.File(filePath, "application/octet-stream", fileName, enableRangeProcessing: true);
        }

        if (PortalDownloadsService.HasConfiguredFallbackSource(downloadsFallbackUrl))
        {
            return Results.Redirect(PortalProxyUtils.ComposeRedirect(downloadsFallbackUrl, path, context.Request.QueryString));
        }

        return Results.NotFound(new
        {
            error = "download_not_found",
            path = path ?? string.Empty
        });
    });
}

app.MapReverseProxy();
app.Run();

public sealed record DownloadReleaseManifest(
    string Version,
    string Channel,
    DateTimeOffset PublishedAt,
    IReadOnlyList<DownloadArtifact> Downloads);

public sealed record DownloadArtifact(
    string Id,
    string Platform,
    string Url,
    string Sha256);
