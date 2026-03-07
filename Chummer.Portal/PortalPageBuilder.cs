using System.Text;

internal static class PortalPageBuilder
{
    public static string BuildDownloadsHtml(string fallbackDownloadsUrl, bool hasFallbackSource)
    {
        string escapedFallbackUrl = HtmlEncode(fallbackDownloadsUrl);
        string escapedScriptFallbackUrl = JavaScriptStringEncode(fallbackDownloadsUrl);
        string fallbackLinkHiddenAttribute = hasFallbackSource ? string.Empty : " hidden";
        string endpointFailureText = hasFallbackSource
            ? "Release manifest request failed; use the configured fallback source while the portal downloads endpoint is unavailable."
            : "Release manifest request failed and no fallback source is configured.";
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
        <div id="empty" class="ghost" hidden>No platform artifacts published yet.</div>
        <a href="{{escapedFallbackUrl}}" id="fallback-link"{{fallbackLinkHiddenAttribute}}>Open configured fallback source</a>
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
          const version = typeof manifest.version === 'string' ? manifest.version : 'unknown';
          const channel = typeof manifest.channel === 'string' ? manifest.channel : 'unknown';
          const published = manifest.publishedAt ? new Date(manifest.publishedAt).toISOString() : 'unknown';
          const downloads = Array.isArray(manifest.downloads) ? manifest.downloads : [];
          const status = typeof manifest.status === 'string' ? manifest.status : 'published';
          const source = typeof manifest.source === 'string' ? manifest.source : 'manifest';
          const message = typeof manifest.message === 'string' ? manifest.message : '';
          const manifestHasFallbackSource = manifest.hasFallbackSource === true;
          if (downloads.length === 0) {
            switch (status) {
              case 'unpublished':
                meta.textContent = `No published desktop builds yet (${channel}).`;
                empty.textContent = message || 'No published desktop builds yet. Run desktop-downloads workflow and deploy the generated bundle.';
                break;
              case 'manifest-empty':
                meta.textContent = `Release manifest is present but empty (${channel}).`;
                empty.textContent = message || 'Release manifest is present but has no platform artifacts.';
                break;
              case 'manifest-missing':
                meta.textContent = 'Release manifest is missing from this portal.';
                empty.textContent = message || 'Self-hosted downloads are not mounted or published on this portal.';
                break;
              case 'manifest-error':
                meta.textContent = 'Release manifest is invalid on this portal.';
                empty.textContent = message || 'Release manifest exists but could not be parsed.';
                break;
              case 'fallback-source':
                meta.textContent = 'Portal is using a configured fallback downloads source.';
                empty.textContent = message || 'Open the configured fallback source while self-hosted downloads are unavailable.';
                break;
              default:
                meta.textContent = `Version ${version} (${channel}) has no downloadable artifacts.`;
                empty.textContent = message || 'Manifest has no platform artifacts.';
                break;
            }

            if (!manifestHasFallbackSource) {
              fallbackLink.hidden = true;
            }

            empty.hidden = false;
            return;
          }

          meta.textContent = source === 'local-files'
            ? `Version ${version} (${channel}) available from locally discovered portal artifacts (${published}).`
            : `Version ${version} (${channel}) published ${published}`;

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
          meta.textContent = '{{endpointFailureText}}';
          empty.textContent = '{{endpointFailureText}}';
          empty.hidden = false;
        }
      })();
    </script>
  </body>
</html>
""";
    }

    public static string BuildAvaloniaPlaceholderHtml(string avaloniaBaseUrl, string downloadsBaseUrl)
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

    public static string BuildLandingHtml(
        string blazorBaseUrl,
        string blazorProxyBaseUrl,
        bool useBlazorProxy,
        string hubBaseUrl,
        string hubProxyBaseUrl,
        bool useHubProxy,
        string sessionBaseUrl,
        string sessionProxyBaseUrl,
        bool useSessionProxy,
        string avaloniaBrowserBaseUrl,
        string avaloniaProxyBaseUrl,
        bool useAvaloniaProxy,
        string apiBaseUrl,
        bool isApiKeyForwardingEnabled,
        bool isPortalOwnerForwardingEnabled,
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
        string hubModeText = useHubProxy
            ? "<code>/hub</code> in-process proxy is active."
            : "<code>/hub</code> currently uses redirect mode.";
        string sessionModeText = useSessionProxy
            ? "<code>/session</code> in-process proxy is active."
            : "<code>/session</code> currently uses redirect mode.";
        string avaloniaModeText = useAvaloniaProxy
            ? "<code>/avalonia</code> in-process proxy is active."
            : "<code>/avalonia</code> currently serves a setup placeholder.";
        html.AppendLine("      <p class=\"lead\">Single landing surface for migration heads. Current milestone proxies <code>/api</code>, <code>/openapi</code>, and <code>/docs</code> in-process; " + blazorModeText + " " + hubModeText + " " + sessionModeText + " " + avaloniaModeText + "</p>");
        html.AppendLine("    </header>");
        html.AppendLine("    <section class=\"grid\">");
        html.AppendLine("      <article class=\"card\">");
        html.AppendLine("        <h2>Blazor Web UI</h2>");
        html.AppendLine("        <p>Interactive server UI over shared presentation state.</p>");
        html.AppendLine("        <a href=\"/blazor/\">Open Blazor</a>");
        html.AppendLine("      </article>");
        html.AppendLine("      <article class=\"card\">");
        html.AppendLine("        <h2>ChummerHub Web</h2>");
        html.AppendLine("        <p>Active hub/discovery head over the shared publication, review, and install seams.</p>");
        html.AppendLine("        <a href=\"/hub/\">Open Hub</a>");
        html.AppendLine("      </article>");
        html.AppendLine("      <article class=\"card\">");
        html.AppendLine("        <h2>Session Web</h2>");
        html.AppendLine("        <p>Mobile/session head over dedicated runtime-state, bundle, and ledger seams.</p>");
        html.AppendLine("        <a href=\"/session/\">Open Session</a>");
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
        html.AppendLine("      <div><code>/docs</code> proxy upstream → " + HtmlEncode(apiBaseUrl) + "</div>");
        html.AppendLine("      <div><code>/blazor</code> " + (useBlazorProxy ? "proxy upstream → " + HtmlEncode(blazorProxyBaseUrl) : "redirect → " + HtmlEncode(blazorBaseUrl)) + "</div>");
        html.AppendLine("      <div><code>/hub</code> " + (useHubProxy ? "proxy upstream → " + HtmlEncode(hubProxyBaseUrl) : "redirect → " + HtmlEncode(hubBaseUrl)) + "</div>");
        html.AppendLine("      <div><code>/session</code> " + (useSessionProxy ? "proxy upstream → " + HtmlEncode(sessionProxyBaseUrl) : "redirect → " + HtmlEncode(sessionBaseUrl)) + "</div>");
        html.AppendLine("      <div><code>/avalonia</code> " + (useAvaloniaProxy ? "proxy upstream → " + HtmlEncode(avaloniaProxyBaseUrl) : "placeholder route at " + HtmlEncode(avaloniaBrowserBaseUrl)) + "</div>");
        html.AppendLine("      <div><code>/downloads</code> " + (useDownloadsProxy ? "proxy upstream → " + HtmlEncode(downloadsProxyBaseUrl) : "local files + manifest with fallback feed → " + HtmlEncode(downloadsBaseUrl)) + "</div>");
        html.AppendLine("      <div><code>X-Api-Key</code> forwarding → " + (isApiKeyForwardingEnabled ? "enabled for internal <code>/api</code>, <code>/openapi</code>, and <code>/docs</code> upstream compatibility only" : "disabled") + "</div>");
        html.AppendLine("      <div><code>Portal owner propagation</code> → " + (isPortalOwnerForwardingEnabled ? "signed authenticated owner headers enabled for hosted/public <code>/api</code>, <code>/openapi</code>, and <code>/docs</code> proxy traffic" : "disabled") + "</div>");
        html.AppendLine("    </footer>");
        html.AppendLine("  </main>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        return html.ToString();
    }

    private static string HtmlEncode(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
    }

    private static string JavaScriptStringEncode(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }
}
