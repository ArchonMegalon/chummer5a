using System.Text;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

string blazorBaseUrl = ResolveSetting("Portal:BlazorBaseUrl", "CHUMMER_PORTAL_BLAZOR_URL", "http://127.0.0.1:8089/");
string blazorProxyBaseUrl = ResolveSetting("Portal:BlazorProxyBaseUrl", "CHUMMER_PORTAL_BLAZOR_PROXY_URL", string.Empty);
string apiBaseUrl = ResolveSetting("Portal:ApiBaseUrl", "CHUMMER_PORTAL_API_URL", "http://chummer-api:8080/");
string docsBaseUrl = ResolveSetting("Portal:DocsBaseUrl", "CHUMMER_PORTAL_DOCS_URL", "http://chummer-api:8080/docs/");
string downloadsBaseUrl = ResolveSetting("Portal:DownloadsBaseUrl", "CHUMMER_PORTAL_DOWNLOADS_URL", "https://github.com/chummer5a/chummer5a/releases/latest");
bool useBlazorProxy = !string.IsNullOrWhiteSpace(blazorProxyBaseUrl);

var proxyRoutes = new List<RouteConfig>
{
    new RouteConfig
    {
        RouteId = "portal-api",
        ClusterId = "api-cluster",
        Match = new RouteMatch
        {
            Path = "/api/{**catch-all}"
        }
    },
    new RouteConfig
    {
        RouteId = "portal-docs",
        ClusterId = "api-cluster",
        Match = new RouteMatch
        {
            Path = "/docs/{**catch-all}"
        }
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
    BuildLandingHtml(blazorBaseUrl, blazorProxyBaseUrl, useBlazorProxy, apiBaseUrl, docsBaseUrl, downloadsBaseUrl),
    "text/html; charset=utf-8"));

app.MapGet("/downloads/releases.json", () => Results.Json(new
{
    version = "nightly",
    channel = "docker",
    publishedAt = DateTimeOffset.UtcNow,
    downloads = Array.Empty<object>()
}));

if (!useBlazorProxy)
{
    app.MapGet("/blazor/{**path}", (HttpContext context, string? path) =>
        Results.Redirect(ComposeRedirect(blazorBaseUrl, path, context.Request.QueryString)));
}

app.MapGet("/downloads/{**path}", (HttpContext context, string? path) =>
    Results.Redirect(ComposeRedirect(downloadsBaseUrl, path, context.Request.QueryString)));

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

static string BuildLandingHtml(
    string blazorBaseUrl,
    string blazorProxyBaseUrl,
    bool useBlazorProxy,
    string apiBaseUrl,
    string docsBaseUrl,
    string downloadsBaseUrl)
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
    html.AppendLine("      <p class=\"lead\">Single landing surface for migration heads. Current milestone proxies <code>/api</code> and <code>/docs</code> in-process; " + blazorModeText + "</p>");
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
    html.AppendLine("        <h2>API Docs</h2>");
    html.AppendLine("        <p>Planned OpenAPI + Scalar endpoint for external consumers.</p>");
    html.AppendLine("        <a href=\"/docs/\">Open Docs</a>");
    html.AppendLine("      </article>");
    html.AppendLine("      <article class=\"card\">");
    html.AppendLine("        <h2>Downloads</h2>");
    html.AppendLine("        <p>Planned desktop release manifest and platform matrix.</p>");
    html.AppendLine("        <a href=\"/downloads/\">Open Downloads</a>");
    html.AppendLine("      </article>");
    html.AppendLine("    </section>");
    html.AppendLine("    <footer>");
    html.AppendLine("      <div><code>/api</code> proxy upstream → " + HtmlEncode(apiBaseUrl) + "</div>");
    html.AppendLine("      <div><code>/docs</code> proxy upstream → " + HtmlEncode(docsBaseUrl) + "</div>");
    html.AppendLine("      <div><code>/blazor</code> " + (useBlazorProxy ? "proxy upstream → " + HtmlEncode(blazorProxyBaseUrl) : "redirect → " + HtmlEncode(blazorBaseUrl)) + "</div>");
    html.AppendLine("      <div><code>/downloads</code> redirect → " + HtmlEncode(downloadsBaseUrl) + "</div>");
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
