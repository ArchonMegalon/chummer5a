using System.Text;
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
string downloadsBaseUrl = ResolveSetting("Portal:DownloadsBaseUrl", "CHUMMER_PORTAL_DOWNLOADS_URL", "https://github.com/ArchonMegalon/chummer5a/releases/latest");
string downloadsProxyBaseUrl = ResolveSetting("Portal:DownloadsProxyBaseUrl", "CHUMMER_PORTAL_DOWNLOADS_PROXY_URL", string.Empty);
string releaseManifestPath = ResolveSetting("Portal:ReleaseManifestPath", "CHUMMER_PORTAL_RELEASES_FILE", "downloads/releases.json");
string releaseFilesPath = ResolveSetting("Portal:ReleaseFilesPath", "CHUMMER_PORTAL_RELEASES_DIR", string.Empty);
string resolvedManifestPath = ResolveManifestPath(releaseManifestPath);
string resolvedReleaseFilesPath = ResolveReleaseFilesPath(releaseFilesPath, resolvedManifestPath);
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
    BuildLandingHtml(
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
    LoadReleaseManifest(resolvedManifestPath, downloadsBaseUrl),
    new JsonSerializerOptions(JsonSerializerDefaults.Web)));

app.MapGet("/downloads/", () => Results.Content(
    BuildDownloadsHtml(downloadsBaseUrl),
    "text/html; charset=utf-8"));

if (!useBlazorProxy)
{
    app.MapGet("/blazor/{**path}", (HttpContext context, string? path) =>
        Results.Redirect(ComposeRedirect(blazorBaseUrl, path, context.Request.QueryString)));
}

if (!useAvaloniaProxy)
{
    app.MapGet("/avalonia/{**path}", () => Results.Content(
        BuildAvaloniaPlaceholderHtml(avaloniaBrowserBaseUrl, downloadsBaseUrl),
        "text/html; charset=utf-8"));
}

if (!useDownloadsProxy)
{
    app.MapGet("/downloads/{**path}", (HttpContext context, string? path) =>
    {
        string? filePath = ResolveDownloadFilePath(resolvedReleaseFilesPath, path);
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

static DownloadReleaseManifest LoadReleaseManifest(string manifestPath, string fallbackDownloadsUrl)
{
    if (!File.Exists(manifestPath))
    {
        return BuildFallbackManifest(fallbackDownloadsUrl);
    }

    try
    {
        string json = File.ReadAllText(manifestPath);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
        DownloadReleaseManifest? manifest = JsonSerializer.Deserialize<DownloadReleaseManifest>(json, options);
        if (manifest is null || string.IsNullOrWhiteSpace(manifest.Version))
        {
            return BuildFallbackManifest(fallbackDownloadsUrl);
        }

        return manifest with
        {
            Downloads = manifest.Downloads ?? Array.Empty<DownloadArtifact>()
        };
    }
    catch
    {
        return BuildFallbackManifest(fallbackDownloadsUrl);
    }
}

static string ResolveManifestPath(string configuredPath)
{
    if (Path.IsPathRooted(configuredPath))
    {
        return configuredPath;
    }

    return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
}

static string ResolveReleaseFilesPath(string configuredPath, string manifestPath)
{
    if (!string.IsNullOrWhiteSpace(configuredPath))
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
    }

    string? fromManifest = Path.GetDirectoryName(manifestPath);
    if (!string.IsNullOrWhiteSpace(fromManifest))
    {
        return fromManifest;
    }

    return AppContext.BaseDirectory;
}

static string? ResolveDownloadFilePath(string rootDirectory, string? path)
{
    if (string.IsNullOrWhiteSpace(path))
    {
        return null;
    }

    string rootPath = Path.GetFullPath(rootDirectory);
    string cleanedPath = path.TrimStart('/').Replace('\\', '/');
    if (cleanedPath.Contains("..", StringComparison.Ordinal))
    {
        return null;
    }

    string localPath = cleanedPath.Replace('/', Path.DirectorySeparatorChar);
    string candidatePath = Path.GetFullPath(Path.Combine(rootPath, localPath));
    if (!candidatePath.StartsWith(rootPath, StringComparison.Ordinal))
    {
        return null;
    }

    if (!File.Exists(candidatePath))
    {
        return null;
    }

    return candidatePath;
}

static DownloadReleaseManifest BuildFallbackManifest(string fallbackDownloadsUrl)
{
    return new DownloadReleaseManifest(
        Version: "nightly",
        Channel: "docker",
        PublishedAt: DateTimeOffset.UtcNow,
        Downloads:
        [
            new DownloadArtifact(
                Id: "latest-release",
                Platform: "Latest release feed",
                Url: fallbackDownloadsUrl,
                Sha256: string.Empty)
        ]);
}

static string BuildDownloadsHtml(string fallbackDownloadsUrl)
{
    string escapedFallbackUrl = HtmlEncode(fallbackDownloadsUrl);
    string escapedScriptFallbackUrl = JavaScriptStringEncode(fallbackDownloadsUrl);
    return $$"""
<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Chummer Downloads</title>
    <style>
      :root { --ink: #f0f3f7; --muted: #b5c2d0; --edge: #32495f; --bg: #071420; --card: #132230; --accent: #f58b2a; }
      * { box-sizing: border-box; }
      body { margin: 0; min-height: 100vh; padding: 24px; color: var(--ink); background: linear-gradient(160deg, #081824, #162736); font-family: "Aptos", "Segoe UI Variable", "Segoe UI", sans-serif; }
      main { width: min(820px, 100%); margin: 0 auto; background: color-mix(in oklab, var(--card) 88%, #000 12%); border: 1px solid var(--edge); border-radius: 14px; box-shadow: 0 18px 40px rgba(0,0,0,0.35); overflow: hidden; }
      header { padding: 18px 20px 10px; border-bottom: 1px solid var(--edge); }
      h1 { margin: 0; font-size: clamp(1.3rem, 2.6vw, 1.9rem); }
      p { margin: 10px 0 0; color: var(--muted); }
      .content { padding: 14px 20px 18px; display: grid; gap: 12px; }
      .meta { font-size: 0.92rem; color: var(--muted); }
      ul { margin: 0; padding: 0; list-style: none; display: grid; gap: 10px; }
      li { border: 1px solid var(--edge); background: rgba(255,255,255,0.02); border-radius: 10px; padding: 10px; display: grid; gap: 6px; }
      a { color: #091016; background: var(--accent); text-decoration: none; font-weight: 700; border-radius: 999px; padding: 7px 11px; justify-self: start; }
      code { font-size: 0.8rem; color: #ffe0c2; }
      .ghost { color: var(--muted); border: 1px solid var(--edge); border-radius: 10px; padding: 12px; }
    </style>
  </head>
  <body>
    <main>
      <header>
        <h1>Desktop Downloads</h1>
        <p>Manifest-backed platform matrix from <code>/downloads/releases.json</code>.</p>
      </header>
      <section class="content">
        <div id="meta" class="meta">Loading release manifest...</div>
        <ul id="download-list"></ul>
        <div id="empty" class="ghost" hidden>No platform artifacts published yet. Use the fallback release feed.</div>
        <a href="{{escapedFallbackUrl}}" id="fallback-link">Open fallback release feed</a>
      </section>
    </main>
    <script>
      (async function () {
        const meta = document.getElementById('meta');
        const list = document.getElementById('download-list');
        const empty = document.getElementById('empty');
        const fallbackLink = document.getElementById('fallback-link');
        fallbackLink.href = '{{escapedScriptFallbackUrl}}';

        try {
          const response = await fetch('/downloads/releases.json', { cache: 'no-store' });
          if (!response.ok) {
            throw new Error('manifest request failed: ' + response.status);
          }

          const manifest = await response.json();
          const published = manifest.publishedAt ? new Date(manifest.publishedAt).toISOString() : 'unknown';
          meta.textContent = `Version ${manifest.version || 'unknown'} (${manifest.channel || 'unknown'}) published ${published}`;

          const downloads = Array.isArray(manifest.downloads) ? manifest.downloads : [];
          if (downloads.length === 0) {
            empty.hidden = false;
            return;
          }

          for (const item of downloads) {
            const row = document.createElement('li');
            const title = document.createElement('strong');
            title.textContent = `${item.platform || 'Artifact'} (${item.id || 'unknown'})`;
            row.appendChild(title);

            if (item.sha256) {
              const hash = document.createElement('code');
              hash.textContent = `sha256: ${item.sha256}`;
              row.appendChild(hash);
            }

            if (item.url) {
              const anchor = document.createElement('a');
              anchor.href = item.url;
              anchor.textContent = 'Download';
              row.appendChild(anchor);
            }

            list.appendChild(row);
          }
        } catch (error) {
          meta.textContent = 'Manifest unavailable; showing fallback release feed.';
          empty.hidden = false;
        }
      })();
    </script>
  </body>
</html>
""";
}

static string BuildAvaloniaPlaceholderHtml(string avaloniaBaseUrl, string downloadsBaseUrl)
{
    string escapedAvaloniaBaseUrl = HtmlEncode(avaloniaBaseUrl);
    string escapedDownloadsBaseUrl = HtmlEncode(downloadsBaseUrl);
    return $$"""
<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Avalonia Browser Entry</title>
    <style>
      :root { --ink: #eef4fb; --muted: #b8c5d1; --accent: #f58b2a; --bg: #0d1f2f; --card: #142739; --edge: #33516e; }
      body { margin: 0; min-height: 100vh; display: grid; place-items: center; padding: 24px; font-family: "Aptos", "Segoe UI", sans-serif; color: var(--ink); background: linear-gradient(155deg, #0c1d2d, #1b3045); }
      main { width: min(760px, 100%); border: 1px solid var(--edge); background: rgba(20, 39, 57, 0.95); border-radius: 14px; padding: 22px; box-shadow: 0 18px 40px rgba(0, 0, 0, 0.35); }
      h1 { margin: 0 0 8px; font-size: clamp(1.3rem, 2.7vw, 1.9rem); }
      p { margin: 8px 0; color: var(--muted); line-height: 1.45; }
      code { color: #ffe0c2; }
      .actions { margin-top: 14px; display: flex; gap: 10px; flex-wrap: wrap; }
      a { text-decoration: none; color: #08131d; background: var(--accent); padding: 8px 12px; border-radius: 999px; font-weight: 700; }
    </style>
  </head>
  <body>
    <main>
      <h1>Avalonia Browser Entry</h1>
      <p>The browser-hosted Avalonia head is not configured in this environment.</p>
      <p>Configure <code>CHUMMER_PORTAL_AVALONIA_PROXY_URL</code> to route <code>/avalonia/*</code> to a hosted Avalonia browser build.</p>
      <p>Current route base: <code>{{escapedAvaloniaBaseUrl}}</code></p>
      <div class="actions">
        <a href="/downloads/">Open Desktop Downloads</a>
        <a href="{{escapedDownloadsBaseUrl}}">Fallback Release Feed</a>
      </div>
    </main>
  </body>
</html>
""";
}

static string BuildLandingHtml(
    string blazorBaseUrl,
    string blazorProxyBaseUrl,
    bool useBlazorProxy,
    string avaloniaBrowserBaseUrl,
    string avaloniaProxyBaseUrl,
    bool useAvaloniaProxy,
    string apiBaseUrl,
    bool isApiKeyForwardingEnabled,
    string docsBaseUrl,
    string downloadsBaseUrl,
    string downloadsProxyBaseUrl,
    bool useDownloadsProxy)
{
    var html = new StringBuilder();
    html.AppendLine("<!doctype html>");
    html.AppendLine("<html lang=\"en\">");
    html.AppendLine("<head>");
    html.AppendLine("  <meta charset=\"utf-8\" />");
    html.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
    html.AppendLine("  <title>Chummer Portal</title>");
    html.AppendLine("  <style>");
    html.AppendLine("    :root {");
    html.AppendLine("      --ink: #f6f6f2;");
    html.AppendLine("      --steel: #cad4de;");
    html.AppendLine("      --accent: #f58b2a;");
    html.AppendLine("      --panel: rgba(6, 22, 33, 0.75);");
    html.AppendLine("      --edge: rgba(245, 139, 42, 0.45);");
    html.AppendLine("      --bg-a: #0b1d2a;");
    html.AppendLine("      --bg-b: #1d2938;");
    html.AppendLine("    }");
    html.AppendLine("    * { box-sizing: border-box; }");
    html.AppendLine("    body {");
    html.AppendLine("      margin: 0;");
    html.AppendLine("      min-height: 100vh;");
    html.AppendLine("      color: var(--ink);");
    html.AppendLine("      font-family: \"Aptos\", \"Segoe UI Variable\", \"Segoe UI\", sans-serif;");
    html.AppendLine("      background: radial-gradient(circle at 12% 18%, #27445b 0%, transparent 40%), linear-gradient(125deg, var(--bg-a), var(--bg-b));");
    html.AppendLine("      display: grid;");
    html.AppendLine("      place-items: center;");
    html.AppendLine("      padding: 24px;");
    html.AppendLine("    }");
    html.AppendLine("    .shell {");
    html.AppendLine("      width: min(980px, 100%);");
    html.AppendLine("      background: var(--panel);");
    html.AppendLine("      border: 1px solid var(--edge);");
    html.AppendLine("      border-radius: 16px;");
    html.AppendLine("      backdrop-filter: blur(6px);");
    html.AppendLine("      box-shadow: 0 16px 50px rgba(0, 0, 0, 0.35);");
    html.AppendLine("      overflow: hidden;");
    html.AppendLine("    }");
    html.AppendLine("    header { padding: 20px 22px 10px; }");
    html.AppendLine("    h1 { margin: 0; font-size: clamp(1.5rem, 3.2vw, 2.2rem); letter-spacing: 0.02em; }");
    html.AppendLine("    p.lead { margin: 10px 0 0; color: var(--steel); line-height: 1.45; }");
    html.AppendLine("    .grid {");
    html.AppendLine("      display: grid;");
    html.AppendLine("      grid-template-columns: repeat(auto-fit, minmax(210px, 1fr));");
    html.AppendLine("      gap: 12px;");
    html.AppendLine("      padding: 14px 22px 22px;");
    html.AppendLine("    }");
    html.AppendLine("    .card {");
    html.AppendLine("      background: rgba(255, 255, 255, 0.03);");
    html.AppendLine("      border: 1px solid rgba(202, 212, 222, 0.22);");
    html.AppendLine("      border-radius: 12px;");
    html.AppendLine("      padding: 14px;");
    html.AppendLine("      display: grid;");
    html.AppendLine("      gap: 8px;");
    html.AppendLine("      min-height: 148px;");
    html.AppendLine("    }");
    html.AppendLine("    .card h2 { margin: 0; font-size: 1.02rem; }");
    html.AppendLine("    .card p { margin: 0; color: var(--steel); line-height: 1.35; font-size: 0.94rem; }");
    html.AppendLine("    .card a {");
    html.AppendLine("      justify-self: start;");
    html.AppendLine("      margin-top: auto;");
    html.AppendLine("      text-decoration: none;");
    html.AppendLine("      color: #091016;");
    html.AppendLine("      background: var(--accent);");
    html.AppendLine("      border-radius: 999px;");
    html.AppendLine("      padding: 8px 12px;");
    html.AppendLine("      font-weight: 700;");
    html.AppendLine("    }");
    html.AppendLine("    footer {");
    html.AppendLine("      border-top: 1px solid rgba(202, 212, 222, 0.2);");
    html.AppendLine("      color: var(--steel);");
    html.AppendLine("      font-size: 0.85rem;");
    html.AppendLine("      padding: 10px 22px 14px;");
    html.AppendLine("      line-height: 1.4;");
    html.AppendLine("    }");
    html.AppendLine("    code { color: #ffd9b1; }");
    html.AppendLine("  </style>");
    html.AppendLine("</head>");
    html.AppendLine("<body>");
    html.AppendLine("  <main class=\"shell\">");
    html.AppendLine("    <header>");
    html.AppendLine("      <h1>Chummer Portal</h1>");
    string blazorModeText = useBlazorProxy
        ? "<code>/blazor</code> in-process proxy is active."
        : "<code>/blazor</code> currently uses redirect mode.";
    string avaloniaModeText = useAvaloniaProxy
        ? "<code>/avalonia</code> in-process proxy is active."
        : "<code>/avalonia</code> currently serves a setup placeholder.";
    html.AppendLine("      <p class=\"lead\">Single landing surface for migration heads. Current milestone proxies <code>/api</code>, <code>/openapi</code>, and <code>/docs</code> in-process; " + blazorModeText + " " + avaloniaModeText + "</p>");
    html.AppendLine("    </header>");
    html.AppendLine("    <section class=\"grid\">");
    html.AppendLine("      <article class=\"card\">");
    html.AppendLine("        <h2>Blazor Web UI</h2>");
    html.AppendLine("        <p>Interactive server UI over shared presentation state.</p>");
    html.AppendLine("        <a href=\"/blazor/\">Open Blazor</a>");
    html.AppendLine("      </article>");
    html.AppendLine("      <article class=\"card\">");
    html.AppendLine("        <h2>API Surface</h2>");
    html.AppendLine("        <p>Headless API host with workspace and command endpoints.</p>");
    html.AppendLine("        <a href=\"/api/health\">Open API</a>");
    html.AppendLine("      </article>");
    html.AppendLine("      <article class=\"card\">");
    html.AppendLine("        <h2>Avalonia Browser</h2>");
    html.AppendLine("        <p>Browser-hosted Avalonia entry path for parity and preview flows.</p>");
    html.AppendLine("        <a href=\"/avalonia/\">Open Avalonia</a>");
    html.AppendLine("      </article>");
    html.AppendLine("      <article class=\"card\">");
    html.AppendLine("        <h2>API Docs</h2>");
    html.AppendLine("        <p>Interactive OpenAPI docs for external consumers.</p>");
    html.AppendLine("        <a href=\"/docs/\">Open Docs</a>");
    html.AppendLine("      </article>");
    html.AppendLine("      <article class=\"card\">");
    html.AppendLine("        <h2>Downloads</h2>");
    html.AppendLine("        <p>Manifest-backed desktop release matrix with fallback feed.</p>");
    html.AppendLine("        <a href=\"/downloads/\">Open Downloads</a>");
    html.AppendLine("      </article>");
    html.AppendLine("    </section>");
    html.AppendLine("    <footer>");
    html.AppendLine("      <div><code>/api</code> proxy upstream → " + HtmlEncode(apiBaseUrl) + "</div>");
    html.AppendLine("      <div><code>/openapi</code> proxy upstream → " + HtmlEncode(apiBaseUrl) + "</div>");
    html.AppendLine("      <div><code>/docs</code> proxy upstream → " + HtmlEncode(docsBaseUrl) + "</div>");
    html.AppendLine("      <div><code>/blazor</code> " + (useBlazorProxy ? "proxy upstream → " + HtmlEncode(blazorProxyBaseUrl) : "redirect → " + HtmlEncode(blazorBaseUrl)) + "</div>");
    html.AppendLine("      <div><code>/avalonia</code> " + (useAvaloniaProxy ? "proxy upstream → " + HtmlEncode(avaloniaProxyBaseUrl) : "placeholder route at " + HtmlEncode(avaloniaBrowserBaseUrl)) + "</div>");
    html.AppendLine("      <div><code>/downloads</code> " + (useDownloadsProxy ? "proxy upstream → " + HtmlEncode(downloadsProxyBaseUrl) : "local files + manifest with fallback feed → " + HtmlEncode(downloadsBaseUrl)) + "</div>");
    html.AppendLine("      <div><code>X-Api-Key</code> forwarding → " + (isApiKeyForwardingEnabled ? "enabled for <code>/api</code>, <code>/openapi</code>, and <code>/docs</code>" : "disabled") + "</div>");
    html.AppendLine("    </footer>");
    html.AppendLine("  </main>");
    html.AppendLine("</body>");
    html.AppendLine("</html>");
    return html.ToString();
}

static string HtmlEncode(string value)
{
    return value
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal);
}

static string JavaScriptStringEncode(string value)
{
    return value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("'", "\\'", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal)
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal);
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
