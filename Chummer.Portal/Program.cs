using System.Text.Json;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

string blazorBaseUrl = ResolveSetting("Portal:BlazorBaseUrl", "CHUMMER_PORTAL_BLAZOR_URL", "http://127.0.0.1:8089/");
string blazorProxyBaseUrl = ResolveSetting("Portal:BlazorProxyBaseUrl", "CHUMMER_PORTAL_BLAZOR_PROXY_URL", string.Empty);
string avaloniaBrowserBaseUrl = ResolveSetting("Portal:AvaloniaBrowserBaseUrl", "CHUMMER_PORTAL_AVALONIA_URL", "/avalonia/");
string avaloniaProxyBaseUrl = ResolveSetting("Portal:AvaloniaProxyBaseUrl", "CHUMMER_PORTAL_AVALONIA_PROXY_URL", string.Empty);
string apiBaseUrl = ResolveSetting("Portal:ApiBaseUrl", "CHUMMER_PORTAL_API_URL", "http://chummer-api:8080/");
string apiProxyKey = ResolveSetting("Portal:ApiKey", "CHUMMER_PORTAL_API_KEY", Environment.GetEnvironmentVariable("CHUMMER_API_KEY") ?? string.Empty);
string docsBaseUrl = ResolveSetting("Portal:DocsBaseUrl", "CHUMMER_PORTAL_DOCS_URL", "http://chummer-api:8080/docs/");
string downloadsBaseUrl = ResolveSetting("Portal:DownloadsBaseUrl", "CHUMMER_PORTAL_DOWNLOADS_URL", "/downloads/");
string downloadsProxyBaseUrl = ResolveSetting("Portal:DownloadsProxyBaseUrl", "CHUMMER_PORTAL_DOWNLOADS_PROXY_URL", string.Empty);
string releaseManifestPath = ResolveSetting("Portal:ReleaseManifestPath", "CHUMMER_PORTAL_RELEASES_FILE", "downloads/releases.json");
string releaseFilesPath = ResolveSetting("Portal:ReleaseFilesPath", "CHUMMER_PORTAL_RELEASES_DIR", string.Empty);
string resolvedManifestPath = PortalDownloadsService.ResolveManifestPath(releaseManifestPath);
string resolvedReleaseFilesPath = PortalDownloadsService.ResolveReleaseFilesPath(releaseFilesPath, resolvedManifestPath);
bool useBlazorProxy = !string.IsNullOrWhiteSpace(blazorProxyBaseUrl);
bool useAvaloniaProxy = !string.IsNullOrWhiteSpace(avaloniaProxyBaseUrl);
bool useDownloadsProxy = !string.IsNullOrWhiteSpace(downloadsProxyBaseUrl);
bool isApiKeyForwardingEnabled = !string.IsNullOrWhiteSpace(apiProxyKey);
IReadOnlyList<IReadOnlyDictionary<string, string>>? apiRouteTransforms = BuildApiRouteTransforms(apiProxyKey);

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
        Transforms = BuildRouteTransforms(apiRouteTransforms)
    },
    new RouteConfig
    {
        RouteId = "portal-docs",
        ClusterId = "docs-cluster",
        Match = new RouteMatch
        {
            Path = "/docs/{**catch-all}"
        },
        Transforms = BuildRouteTransforms(apiRouteTransforms, "/docs")
    },
    new RouteConfig
    {
        RouteId = "portal-openapi",
        ClusterId = "api-cluster",
        Match = new RouteMatch
        {
            Path = "/openapi/{**catch-all}"
        },
        Transforms = BuildRouteTransforms(apiRouteTransforms)
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
                Address = NormalizeProxyAddress(apiBaseUrl)
            }
        }
    },
    new ClusterConfig
    {
        ClusterId = "docs-cluster",
        Destinations = new Dictionary<string, DestinationConfig>(StringComparer.Ordinal)
        {
            ["primary"] = new DestinationConfig
            {
                Address = NormalizeProxyAddress(docsBaseUrl)
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
                Address = NormalizeProxyAddress(blazorProxyBaseUrl)
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
                Address = NormalizeProxyAddress(avaloniaProxyBaseUrl)
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
                Address = NormalizeProxyAddress(downloadsProxyBaseUrl)
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
        docsBaseUrl,
        downloadsBaseUrl,
        downloadsProxyBaseUrl,
        useDownloadsProxy),
    "text/html; charset=utf-8"));

app.MapGet("/downloads/releases.json", () => Results.Json(
    PortalDownloadsService.LoadReleaseManifest(resolvedManifestPath, downloadsBaseUrl),
    new JsonSerializerOptions(JsonSerializerDefaults.Web)));

app.MapGet("/downloads/", () => Results.Content(
    PortalPageBuilder.BuildDownloadsHtml(downloadsBaseUrl, PortalDownloadsService.HasConfiguredFallbackSource(downloadsBaseUrl)),
    "text/html; charset=utf-8"));

if (!useBlazorProxy)
{
    app.MapGet("/blazor/{**path}", (HttpContext context, string? path) =>
        Results.Redirect(ComposeRedirect(blazorBaseUrl, path, context.Request.QueryString)));
}

if (!useAvaloniaProxy)
{
    app.MapGet("/avalonia/{**path}", () => Results.Content(
        PortalPageBuilder.BuildAvaloniaPlaceholderHtml(avaloniaBrowserBaseUrl, downloadsBaseUrl),
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

        return Results.Redirect(ComposeRedirect(downloadsBaseUrl, path, context.Request.QueryString));
    });
}

app.MapReverseProxy();
app.Run();

string ResolveSetting(string key, string envVar, string fallback)
{
    string? configured = builder.Configuration[key];
    if (!string.IsNullOrWhiteSpace(configured))
    {
        return configured;
    }

    string? environment = Environment.GetEnvironmentVariable(envVar);
    if (!string.IsNullOrWhiteSpace(environment))
    {
        return environment;
    }

    return fallback;
}

static string NormalizeProxyAddress(string baseUrl)
{
    if (Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? absoluteBase))
    {
        string normalized = absoluteBase.ToString();
        return normalized.EndsWith("/", StringComparison.Ordinal) ? normalized : $"{normalized}/";
    }

    return baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : $"{baseUrl}/";
}

static IReadOnlyList<IReadOnlyDictionary<string, string>>? BuildApiRouteTransforms(string apiKey)
{
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        return null;
    }

    return new[]
    {
        (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["RequestHeader"] = "X-Api-Key",
            ["Set"] = apiKey
        }
    };
}

static IReadOnlyList<IReadOnlyDictionary<string, string>>? BuildRouteTransforms(
    IReadOnlyList<IReadOnlyDictionary<string, string>>? apiRouteTransforms,
    string? pathRemovePrefix = null)
{
    List<IReadOnlyDictionary<string, string>> transforms = new();

    if (!string.IsNullOrWhiteSpace(pathRemovePrefix))
    {
        transforms.Add(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["PathRemovePrefix"] = pathRemovePrefix
        });
    }

    if (apiRouteTransforms is not null)
    {
        transforms.AddRange(apiRouteTransforms);
    }

    return transforms.Count == 0 ? null : transforms;
}

static string ComposeRedirect(string baseUrl, string? path, QueryString queryString)
{
    if (Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? absoluteBase))
    {
        string cleanPath = string.IsNullOrWhiteSpace(path) ? string.Empty : path.TrimStart('/');
        Uri redirected = new(absoluteBase, cleanPath);
        return $"{redirected}{queryString}";
    }

    string normalizedBase = baseUrl.TrimEnd('/');
    string suffix = string.IsNullOrWhiteSpace(path) ? string.Empty : $"/{path.TrimStart('/')}";
    return $"{normalizedBase}{suffix}{queryString}";
}

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
