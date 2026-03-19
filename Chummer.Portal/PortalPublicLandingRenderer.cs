using System.Net;
using System.Text;

internal static class PortalPublicLandingRenderer
{
    public static string BuildLandingPage(PortalLandingSurface surface, PortalPublicLandingService service)
    {
        var body = new StringBuilder()
            .AppendLine(RenderHero(surface))
            .AppendLine(RenderCardSection(surface, service, "what_you_can_do_today"))
            .AppendLine(RenderCardSection(surface, service, "why_this_feels_different"))
            .AppendLine(RenderCardSection(surface, service, "choose_your_lane"))
            .AppendLine(RenderCardSection(surface, service, "whats_real_now"))
            .AppendLine(RenderCardSection(surface, service, "coming_next"))
            .AppendLine(RenderCardSection(surface, service, "featured_artifacts"))
            .AppendLine(RenderCardSection(surface, service, "participate"))
            .AppendLine(RenderCardSection(surface, service, "release_shelf"));
        return RenderPage(surface, "Chummer", surface.Subhead, body.ToString());
    }

    public static string BuildStoryPage(PortalLandingSurface surface, PortalPublicLandingService service)
    {
        var body = new StringBuilder()
            .AppendLine("""
<section class="panel prose">
  <h2>What the product is trying to become</h2>
  <p>Chummer is trying to become the place where rules truth, session continuity, public proof, and future artifact lanes feel like one coherent product instead of a bag of unrelated tools.</p>
  <p>The front door should tell you what is real, what is coming, and why it is worth trusting before it asks you to learn any internal language.</p>
</section>
""")
            .AppendLine(RenderCardSection(surface, service, "why_this_feels_different"))
            .AppendLine(RenderCardSection(surface, service, "choose_your_lane"));
        return RenderPage(surface, "What Is Chummer?", surface.ProofLine, body.ToString());
    }

    public static string BuildNowPage(PortalLandingSurface surface, PortalPublicLandingService service)
    {
        var body = new StringBuilder()
            .AppendLine(RenderCardSection(surface, service, "whats_real_now"))
            .AppendLine(RenderCardSection(surface, service, "release_shelf"));
        return RenderPage(surface, "What Is Real Today", "This page exists to prove there is something real here now, not to promise magic later.", body.ToString());
    }

    public static string BuildHorizonsPage(PortalLandingSurface surface, PortalPublicLandingService service)
        => RenderPage(
            surface,
            "Coming Next",
            "Horizons are real future lanes with canonical names, pain statements, and payoff promises. They are not shipment lies.",
            RenderCardSection(surface, service, "coming_next"));

    public static string BuildArtifactsPage(PortalLandingSurface surface, PortalPublicLandingService service)
        => RenderPage(
            surface,
            "Featured Artifacts",
            "Teasers make future lanes tangible without pretending they are already done.",
            RenderCardSection(surface, service, "featured_artifacts"));

    public static string BuildParticipatePage(PortalLandingSurface surface, PortalPublicLandingService service, bool isAuthenticated)
    {
        var authBlock = isAuthenticated
            ? """
<section class="panel prose">
  <h2>Signed-in preview</h2>
  <p>You are already through the lightweight portal gate. This preview still keeps the actual booster execution lane bounded, review-governed, and separate from ordinary public feedback.</p>
  <p><a class="inline-link" href="/home">Open your signed-in home</a></p>
</section>
"""
            : """
<section class="panel prose">
  <h2>Before you participate</h2>
  <p>You do not need a premium subscription to help the project. Public bugs, feedback, and future signals are still valid. The booster path exists only for people who explicitly want to lend temporary premium coding capacity.</p>
  <p>The friendlier sign-in head is still catching up, so this route stays explanatory first.</p>
</section>
""";

        var body = new StringBuilder()
            .AppendLine("""
<section class="panel prose">
  <h2>How participation works</h2>
  <ol>
    <li>Use public feedback when you only want to report a bug or suggest a future.</li>
    <li>Use the booster path only when you explicitly want to lend temporary premium coding capacity.</li>
    <li>Hub keeps the user, group, receipt, reward, and entitlement truth.</li>
    <li>Fleet opens the temporary worker lane and handles device-code auth on the worker host.</li>
    <li>Final landing still goes through review and jury. Participation does not bypass governance.</li>
  </ol>
</section>
""")
            .AppendLine(authBlock)
            .AppendLine(RenderCardSection(surface, service, "participate"));
        return RenderPage(surface, "Participate", "There are two clean help lanes: public feedback and the bounded booster path.", body.ToString());
    }

    public static string BuildStatusPage(PortalLandingSurface surface, PortalPublicLandingService service)
    {
        var availableCount = service.CardsForBucket(surface, "whats_real_now").Count;
        var horizonCount = service.CardsForBucket(surface, "coming_next").Count;
        var artifactCount = service.CardsForBucket(surface, "featured_artifacts").Count;
        var body = $$"""
<section class="stats-strip">
  <article><span class="eyebrow">Available now</span><strong>{{availableCount}}</strong></article>
  <article><span class="eyebrow">Horizons</span><strong>{{horizonCount}}</strong></article>
  <article><span class="eyebrow">Artifact teasers</span><strong>{{artifactCount}}</strong></article>
  <article><span class="eyebrow">Registered overlays</span><strong>{{surface.RegisteredOverlays.Count}}</strong></article>
</section>
{{RenderCardSection(surface, service, "whats_real_now")}}
{{RenderCardSection(surface, service, "coming_next")}}
""";
        return RenderPage(surface, "Public Status", "Public status moves from canonical design and visible proof cards, not from repo archaeology.", body);
    }

    public static string BuildHomePage(PortalLandingSurface surface, bool isAuthenticated, string? displayName)
    {
        string body = isAuthenticated
            ? $$"""
<section class="panel prose">
  <h2>Welcome back</h2>
  <p><strong>{{Encode(displayName ?? "Portal user")}}</strong> is signed in.</p>
  <ul>
    <li>This is the right home for follows, beta interest, participation state, and future advisory overlays.</li>
    <li>The private account plane is still intentionally thin in this preview.</li>
  </ul>
  <p><a class="inline-link" href="/account">Account</a> · <a class="inline-link" href="/participate">Participate</a> · <a class="inline-link" href="/status">Status</a></p>
</section>
{{RenderOverlaySection(surface)}}
"""
            : $$"""
<section class="panel prose">
  <h2>Sign in to unlock overlays</h2>
  <p>This preview keeps sign-in light on purpose. When a portal-auth session exists, this route becomes the home for follows, beta interest, participation state, and future advisory surfaces.</p>
  <p><a class="inline-link" href="/participate">Participate</a> · <a class="inline-link" href="/status">Status</a></p>
</section>
{{RenderOverlaySection(surface)}}
""";
        return RenderPage(surface, "Home", "Sign in unlocks follows, beta interest, and the bounded booster lane.", body);
    }

    public static string BuildAccountPage(PortalLandingSurface surface, bool isAuthenticated, string? displayName)
    {
        string body = isAuthenticated
            ? $$"""
<section class="panel prose">
  <h2>Account preview</h2>
  <p>Signed in as <strong>{{Encode(displayName ?? "Portal user")}}</strong>.</p>
  <p>The full profile model lives in the newer hosted split, but this route exists now so the landing has a real signed-in destination instead of a lie.</p>
  <ul>
    <li>Interest flags: player, GM, creator</li>
    <li>Future follows: horizons and beta invites</li>
    <li>Participation posture: public feedback vs bounded booster</li>
  </ul>
</section>
"""
            : """
<section class="panel prose">
  <h2>Account preview</h2>
  <p>This route is intentionally quiet until a real sign-in session exists.</p>
  <p>The public front door stays readable without forcing a giant onboarding wizard.</p>
</section>
""";
        return RenderPage(surface, "Account", "Registered users eventually get lightweight profile and follow surfaces here.", body);
    }

    private static string RenderHero(PortalLandingSurface surface)
    {
        var ctas = string.Join("", surface.HeroCtas.Select(action =>
            $"""<a class="cta {(action.Emphasis == "secondary" ? "secondary" : string.Empty)}" href="{EncodeHref(action.Href)}">{Encode(action.Label)}</a>"""));
        var highlights = string.Join("", surface.SecondaryHighlights.Select(item => $"""<li>{Encode(item)}</li>"""));
        return $$"""
<section class="hero">
  <div class="hero-copy">
    <p class="eyebrow">Chummer</p>
    <h1>{{Encode(surface.Headline)}}</h1>
    <p class="lead">{{Encode(surface.Subhead)}}</p>
    <div class="cta-row">{{ctas}}</div>
    <ul class="highlights">{{highlights}}</ul>
  </div>
  <div class="hero-art" aria-hidden="true">
    <div class="poster">
      <span>proof shelf</span>
      <strong>local-first</strong>
      <span>coming next</span>
    </div>
  </div>
</section>
""";
    }

    private static string RenderOverlaySection(PortalLandingSurface surface)
    {
        var overlays = string.Join("", surface.RegisteredOverlays.Select(overlay => $$"""
<article class="card compact">
  <span class="badge">Registered</span>
  <h3><a href="{{EncodeHref(overlay.Path)}}">{{Encode(overlay.Title)}}</a></h3>
  <p>{{Encode(overlay.Summary)}}</p>
</article>
"""));
        return $$"""
<section class="section-block">
  <div class="section-header">
    <p class="eyebrow">Registered overlays</p>
    <h2>What changes when you sign in</h2>
  </div>
  <div class="card-grid">{{overlays}}</div>
</section>
""";
    }

    private static string RenderCardSection(PortalLandingSurface surface, PortalPublicLandingService service, string bucket)
    {
        var section = surface.Sections.FirstOrDefault(item => string.Equals(item.Id, bucket, StringComparison.Ordinal));
        var cards = service.CardsForBucket(surface, bucket);
        if (section is null || cards.Count == 0)
        {
            return string.Empty;
        }

        var cardHtml = string.Join("", cards.Select(card => $$"""
<article class="card">
  <div class="media-chip">{{Encode(card.ImageFamily.Replace('_', ' '))}}</div>
  <span class="badge">{{Encode(card.Badge)}}</span>
  <h3><a href="{{EncodeHref(card.Href)}}">{{Encode(card.Title)}}</a></h3>
  <p>{{Encode(card.Summary)}}</p>
  {{(string.IsNullOrWhiteSpace(card.Pain) ? string.Empty : $"<p class=\"micro\"><strong>Pain:</strong> {Encode(card.Pain)}</p>")}}
  {{(string.IsNullOrWhiteSpace(card.Payoff) ? string.Empty : $"<p class=\"micro\"><strong>Payoff:</strong> {Encode(card.Payoff)}</p>")}}
</article>
"""));

        return $$"""
<section class="section-block" id="{{Encode(section.Id)}}">
  <div class="section-header">
    <p class="eyebrow">{{Encode(section.Route)}}</p>
    <h2>{{Encode(section.Title)}}</h2>
  </div>
  <div class="card-grid">{{cardHtml}}</div>
</section>
""";
    }

    private static string RenderPage(PortalLandingSurface surface, string title, string lead, string body)
    {
        var nav = string.Join("", surface.PublicRoutes.Select(route =>
            $"""<a href="{EncodeHref(route.Path)}">{Encode(route.Title)}</a>"""));

        return $$"""
<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>{{Encode(title)}} · Chummer</title>
    <style>
      :root {
        --ink: #f4ecd9;
        --muted: #c4c0b6;
        --accent: #d96b2b;
        --accent-soft: rgba(217, 107, 43, 0.18);
        --paper: rgba(17, 20, 18, 0.88);
        --panel: rgba(24, 28, 26, 0.92);
        --edge: rgba(217, 107, 43, 0.28);
        --shadow: rgba(0, 0, 0, 0.35);
        --bg-a: #11181b;
        --bg-b: #24343a;
        --bg-c: #5a2f1c;
      }
      * { box-sizing: border-box; }
      body {
        margin: 0;
        min-height: 100vh;
        color: var(--ink);
        font-family: "Aptos", "Segoe UI Variable", "Segoe UI", sans-serif;
        background:
          radial-gradient(circle at 12% 18%, rgba(217, 107, 43, 0.22), transparent 34%),
          radial-gradient(circle at 82% 14%, rgba(112, 148, 147, 0.2), transparent 28%),
          linear-gradient(145deg, var(--bg-a), var(--bg-b) 54%, var(--bg-c));
        padding: 24px;
      }
      .shell {
        width: min(1180px, 100%);
        margin: 0 auto;
        background: var(--paper);
        border: 1px solid var(--edge);
        border-radius: 28px;
        box-shadow: 0 24px 72px var(--shadow);
        overflow: hidden;
        backdrop-filter: blur(8px);
      }
      .topbar {
        display: flex;
        gap: 12px;
        justify-content: space-between;
        align-items: center;
        padding: 18px 24px;
        border-bottom: 1px solid rgba(255,255,255,0.08);
        background: rgba(7, 9, 8, 0.35);
      }
      .brand {
        font-family: "Iowan Old Style", "Palatino Linotype", Georgia, serif;
        font-size: 1.15rem;
        letter-spacing: 0.06em;
        text-transform: uppercase;
      }
      nav {
        display: flex;
        gap: 14px;
        flex-wrap: wrap;
      }
      nav a, .inline-link {
        color: var(--ink);
        text-decoration: none;
        border-bottom: 1px solid transparent;
      }
      nav a:hover, .inline-link:hover {
        border-bottom-color: var(--accent);
      }
      .page {
        padding: 28px 24px 30px;
        display: grid;
        gap: 24px;
      }
      .intro h1 {
        margin: 0;
        font-family: "Iowan Old Style", "Palatino Linotype", Georgia, serif;
        font-size: clamp(2rem, 4vw, 3.6rem);
        line-height: 0.96;
        max-width: 14ch;
      }
      .intro .lead {
        margin: 14px 0 0;
        color: var(--muted);
        max-width: 68ch;
        line-height: 1.55;
        font-size: 1.03rem;
      }
      .hero {
        display: grid;
        grid-template-columns: minmax(0, 1.2fr) minmax(280px, 0.8fr);
        gap: 20px;
        align-items: stretch;
      }
      .hero-copy, .hero-art, .panel {
        background: var(--panel);
        border: 1px solid rgba(255,255,255,0.08);
        border-radius: 22px;
        padding: 22px;
      }
      .eyebrow {
        display: inline-block;
        color: #e0b28d;
        text-transform: uppercase;
        letter-spacing: 0.12em;
        font-size: 0.78rem;
      }
      .hero-copy .lead {
        margin: 12px 0 0;
      }
      .cta-row {
        display: flex;
        flex-wrap: wrap;
        gap: 10px;
        margin-top: 18px;
      }
      .cta {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        padding: 10px 14px;
        border-radius: 999px;
        text-decoration: none;
        background: var(--accent);
        color: #150d08;
        font-weight: 700;
      }
      .cta.secondary {
        background: transparent;
        color: var(--ink);
        border: 1px solid rgba(255,255,255,0.15);
      }
      .highlights {
        margin: 18px 0 0;
        padding: 0;
        list-style: none;
        display: grid;
        gap: 8px;
        color: var(--muted);
      }
      .poster {
        min-height: 100%;
        border-radius: 18px;
        border: 1px solid rgba(255,255,255,0.12);
        background:
          linear-gradient(180deg, rgba(255,255,255,0.02), rgba(255,255,255,0.08)),
          radial-gradient(circle at 30% 22%, rgba(217, 107, 43, 0.28), transparent 32%),
          linear-gradient(160deg, #182327, #0c1113 60%, #3d2418);
        display: grid;
        place-items: center;
        gap: 14px;
        text-transform: uppercase;
        letter-spacing: 0.18em;
        color: #f0d7bf;
        padding: 28px;
        text-align: center;
      }
      .poster strong {
        font-family: "Iowan Old Style", "Palatino Linotype", Georgia, serif;
        font-size: clamp(1.6rem, 3vw, 2.5rem);
        letter-spacing: 0.04em;
      }
      .section-block {
        display: grid;
        gap: 14px;
      }
      .section-header h2, .prose h2 {
        margin: 4px 0 0;
        font-family: "Iowan Old Style", "Palatino Linotype", Georgia, serif;
        font-size: clamp(1.35rem, 2vw, 1.9rem);
      }
      .card-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
        gap: 14px;
      }
      .card {
        display: grid;
        gap: 10px;
        min-height: 210px;
        padding: 18px;
        background: var(--panel);
        border: 1px solid rgba(255,255,255,0.08);
        border-radius: 18px;
      }
      .card.compact {
        min-height: 0;
      }
      .media-chip {
        justify-self: start;
        padding: 6px 10px;
        border-radius: 999px;
        font-size: 0.76rem;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        background: rgba(255,255,255,0.04);
        color: var(--muted);
      }
      .badge {
        justify-self: start;
        padding: 4px 9px;
        border-radius: 999px;
        background: var(--accent-soft);
        color: #f6be95;
        font-size: 0.78rem;
        font-weight: 700;
      }
      .card h3 {
        margin: 0;
        font-size: 1.08rem;
      }
      .card h3 a {
        color: var(--ink);
        text-decoration: none;
      }
      .card p {
        margin: 0;
        color: var(--muted);
        line-height: 1.45;
      }
      .card .micro {
        font-size: 0.9rem;
      }
      .prose p, .prose li {
        color: var(--muted);
        line-height: 1.55;
      }
      .stats-strip {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: 12px;
      }
      .stats-strip article {
        background: var(--panel);
        border: 1px solid rgba(255,255,255,0.08);
        border-radius: 18px;
        padding: 18px;
        display: grid;
        gap: 8px;
      }
      .stats-strip strong {
        font-size: clamp(1.4rem, 3vw, 2.1rem);
      }
      footer {
        border-top: 1px solid rgba(255,255,255,0.08);
        padding: 16px 24px 20px;
        color: var(--muted);
        font-size: 0.82rem;
        line-height: 1.5;
      }
      footer small {
        display: block;
      }
      @media (max-width: 820px) {
        body { padding: 12px; }
        .topbar, .page, footer { padding-left: 16px; padding-right: 16px; }
        .hero { grid-template-columns: 1fr; }
      }
    </style>
  </head>
  <body>
    <main class="shell">
      <div class="topbar">
        <div class="brand">Chummer</div>
        <nav>{{nav}}</nav>
      </div>
      <section class="page">
        <header class="intro">
          <p class="eyebrow">{{Encode(surface.Surface)}}</p>
          <h1>{{Encode(title)}}</h1>
          <p class="lead">{{Encode(lead)}}</p>
        </header>
        {{body}}
      </section>
      <footer>
        <small>Last updated: {{Encode(DateTime.UtcNow.ToString("yyyy-MM-dd"))}}</small>
        <small>Derived from: {{Encode(surface.FooterGeneratedNote)}}</small>
        <small>Canonical source: {{Encode(surface.FooterCanonicalSource)}}</small>
      </footer>
    </main>
  </body>
</html>
""";
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string EncodeHref(string href) => WebUtility.HtmlEncode(href);
}
