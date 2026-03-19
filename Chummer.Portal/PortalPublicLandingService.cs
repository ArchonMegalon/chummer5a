internal sealed record PortalLandingAction(
    string Label,
    string Href,
    string Emphasis);

internal sealed record PortalLandingRoute(
    string Path,
    string Title,
    string Audience,
    string Purpose);

internal sealed record PortalLandingSection(
    string Id,
    string Title,
    string Audience,
    string Route);

internal sealed record PortalLandingOverlay(
    string Id,
    string Path,
    string Title,
    string Summary);

internal sealed record PortalFeatureCard(
    string Id,
    string Bucket,
    string Title,
    string Summary,
    string Href,
    string Badge,
    string Audience,
    string ImageFamily,
    string? Pain = null,
    string? Payoff = null);

internal sealed record PortalLandingSurface(
    string Product,
    string Surface,
    int Version,
    string Headline,
    string Subhead,
    string ProofLine,
    bool NoProviderNames,
    bool NoLtdNames,
    IReadOnlyList<PortalLandingAction> HeroCtas,
    IReadOnlyList<string> SecondaryHighlights,
    IReadOnlyList<PortalLandingRoute> PublicRoutes,
    IReadOnlyList<PortalLandingRoute> RegisteredRoutes,
    IReadOnlyList<PortalLandingSection> Sections,
    IReadOnlyList<PortalLandingOverlay> RegisteredOverlays,
    string FooterCanonicalSource,
    string FooterGeneratedNote,
    IReadOnlyList<PortalFeatureCard> FeatureCards);

internal sealed class PortalPublicLandingService
{
    private const string ManifestRelativePath = "products/chummer/PUBLIC_LANDING_MANIFEST.yaml";
    private const string FeatureRegistryRelativePath = "products/chummer/PUBLIC_FEATURE_REGISTRY.yaml";

    private readonly string _canonRoot;

    public PortalPublicLandingService(string canonRoot)
    {
        _canonRoot = string.IsNullOrWhiteSpace(canonRoot) ? "/design-canon" : canonRoot.Trim();
    }

    public PortalLandingSurface LoadSurface()
    {
        IReadOnlyList<string> manifest;
        IReadOnlyList<string> featureRegistry;
        var manifestPath = Path.Combine(_canonRoot, ManifestRelativePath);
        var featureRegistryPath = Path.Combine(_canonRoot, FeatureRegistryRelativePath);

        if (File.Exists(manifestPath) && File.Exists(featureRegistryPath))
        {
            manifest = File.ReadAllLines(manifestPath);
            featureRegistry = File.ReadAllLines(featureRegistryPath);
        }
        else
        {
            manifest = SplitLines(DefaultManifestYaml);
            featureRegistry = SplitLines(DefaultFeatureRegistryYaml);
        }

        return new PortalLandingSurface(
            Product: RequiredScalar(manifest, "product"),
            Surface: RequiredScalar(manifest, "surface"),
            Version: ParseInt(RequiredScalar(manifest, "version"), "version"),
            Headline: RequiredScalar(manifest, "headline"),
            Subhead: RequiredScalar(manifest, "subhead"),
            ProofLine: RequiredScalar(manifest, "proof_line"),
            NoProviderNames: ParseBool(RequiredScalar(manifest, "no_provider_names")),
            NoLtdNames: ParseBool(RequiredScalar(manifest, "no_ltd_names")),
            HeroCtas: ParseMapList(manifest, "hero_ctas")
                .Select(static item => new PortalLandingAction(
                    Label: Required(item, "label"),
                    Href: Required(item, "href"),
                    Emphasis: Required(item, "emphasis")))
                .ToArray(),
            SecondaryHighlights: ParseStringList(manifest, "secondary_highlights").ToArray(),
            PublicRoutes: ParseMapList(manifest, "public_routes")
                .Select(static item => new PortalLandingRoute(
                    Path: Required(item, "path"),
                    Title: Required(item, "title"),
                    Audience: Required(item, "audience"),
                    Purpose: Required(item, "purpose")))
                .ToArray(),
            RegisteredRoutes: ParseMapList(manifest, "registered_routes")
                .Select(static item => new PortalLandingRoute(
                    Path: Required(item, "path"),
                    Title: Required(item, "title"),
                    Audience: Required(item, "audience"),
                    Purpose: Required(item, "purpose")))
                .ToArray(),
            Sections: ParseMapList(manifest, "sections")
                .Select(static item => new PortalLandingSection(
                    Id: Required(item, "id"),
                    Title: Required(item, "title"),
                    Audience: Required(item, "audience"),
                    Route: Required(item, "route")))
                .ToArray(),
            RegisteredOverlays: ParseMapList(manifest, "registered_overlays")
                .Select(static item => new PortalLandingOverlay(
                    Id: Required(item, "id"),
                    Path: Required(item, "path"),
                    Title: Required(item, "title"),
                    Summary: Required(item, "summary")))
                .ToArray(),
            FooterCanonicalSource: RequiredScalar(manifest, "footer_canonical_source"),
            FooterGeneratedNote: RequiredScalar(manifest, "footer_generated_note"),
            FeatureCards: ParseMapList(featureRegistry, "cards")
                .Select(static item => new PortalFeatureCard(
                    Id: Required(item, "id"),
                    Bucket: Required(item, "bucket"),
                    Title: Required(item, "title"),
                    Summary: Required(item, "summary"),
                    Href: Required(item, "href"),
                    Badge: Required(item, "badge"),
                    Audience: Required(item, "audience"),
                    ImageFamily: Required(item, "image_family"),
                    Pain: Optional(item, "pain"),
                    Payoff: Optional(item, "payoff")))
                .ToArray());
    }

    public IReadOnlyList<PortalFeatureCard> CardsForBucket(PortalLandingSurface surface, string bucket)
        => surface.FeatureCards
            .Where(card => string.Equals(card.Bucket, bucket, StringComparison.Ordinal))
            .ToArray();

    private static IReadOnlyList<string> SplitLines(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.None);

    private static string RequiredScalar(IReadOnlyList<string> lines, string key)
        => OptionalScalar(lines, key) ?? throw new InvalidOperationException($"required landing scalar missing: {key}");

    private static string? OptionalScalar(IReadOnlyList<string> lines, string key)
    {
        foreach (var rawLine in lines)
        {
            var line = StripComment(rawLine);
            if (Indent(line) != 0 || string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!line.StartsWith($"{key}:", StringComparison.Ordinal))
            {
                continue;
            }

            var value = line[(key.Length + 1)..].Trim();
            return string.IsNullOrWhiteSpace(value) ? null : Unquote(value);
        }

        return null;
    }

    private static IReadOnlyList<string> ParseStringList(IReadOnlyList<string> lines, string sectionName)
    {
        var results = new List<string>();
        var insideSection = false;
        foreach (var rawLine in lines)
        {
            var line = StripComment(rawLine);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var indent = Indent(line);
            var trimmed = line.Trim();
            if (indent == 0)
            {
                if (string.Equals(trimmed, $"{sectionName}:", StringComparison.Ordinal))
                {
                    insideSection = true;
                    continue;
                }

                if (insideSection)
                {
                    break;
                }
            }

            if (!insideSection || indent != 2 || !trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                continue;
            }

            results.Add(Unquote(trimmed[2..].Trim()));
        }

        return results;
    }

    private static IReadOnlyList<Dictionary<string, string>> ParseMapList(IReadOnlyList<string> lines, string sectionName)
    {
        var results = new List<Dictionary<string, string>>();
        var insideSection = false;
        Dictionary<string, string>? current = null;

        foreach (var rawLine in lines)
        {
            var line = StripComment(rawLine);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var indent = Indent(line);
            var trimmed = line.Trim();
            if (indent == 0)
            {
                if (string.Equals(trimmed, $"{sectionName}:", StringComparison.Ordinal))
                {
                    insideSection = true;
                    current = null;
                    continue;
                }

                if (insideSection)
                {
                    break;
                }
            }

            if (!insideSection)
            {
                continue;
            }

            if (indent == 2 && trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                current = new Dictionary<string, string>(StringComparer.Ordinal);
                results.Add(current);
                var inline = trimmed[2..].Trim();
                if (!string.IsNullOrWhiteSpace(inline))
                {
                    ParseKeyValueInto(current, inline);
                }

                continue;
            }

            if (indent >= 4 && current is not null)
            {
                ParseKeyValueInto(current, trimmed);
            }
        }

        return results;
    }

    private static void ParseKeyValueInto(Dictionary<string, string> target, string line)
    {
        var separator = line.IndexOf(':');
        if (separator <= 0)
        {
            return;
        }

        var key = line[..separator].Trim();
        var value = line[(separator + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        target[key] = Unquote(value);
    }

    private static string StripComment(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return line;
        }

        var commentIndex = line.IndexOf(" #", StringComparison.Ordinal);
        return commentIndex >= 0 ? line[..commentIndex] : line;
    }

    private static int Indent(string line)
    {
        var count = 0;
        while (count < line.Length && line[count] == ' ')
        {
            count++;
        }

        return count;
    }

    private static string Unquote(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length >= 2
            && ((trimmed[0] == '"' && trimmed[^1] == '"')
                || (trimmed[0] == '\'' && trimmed[^1] == '\'')))
        {
            return trimmed[1..^1];
        }

        return trimmed;
    }

    private static string Required(IReadOnlyDictionary<string, string> values, string key)
        => Optional(values, key) ?? throw new InvalidOperationException($"required landing field missing: {key}");

    private static string? Optional(IReadOnlyDictionary<string, string> values, string key)
        => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static int ParseInt(string value, string name)
        => int.TryParse(value, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"landing integer field '{name}' is invalid: {value}");

    private static bool ParseBool(string value)
        => bool.TryParse(value, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"landing boolean field is invalid: {value}");

    private const string DefaultManifestYaml = """
product: chummer
surface: chummer.run
version: 1
headline: Shadowrun rules truth, with receipts.
subhead: Build, inspect, play, and follow what is coming next without mystery math or empty promises.
proof_line: Local-first continuity, deterministic explainability, and a real public proof shelf belong on the same front door.
no_provider_names: true
no_ltd_names: true
hero_ctas:
  - label: See what is real now
    href: /now
    emphasis: primary
  - label: Get the POC
    href: /downloads
    emphasis: primary
  - label: Sign in to follow what is coming
    href: /home
    emphasis: secondary
secondary_highlights:
  - Runs local-first.
  - Explains the math.
  - Grows into runsites, dossiers, creator tools, and replay.
public_routes:
  - path: /
    title: Landing
    audience: public
    purpose: product_front_door
  - path: /what-is-chummer
    title: What is Chummer?
    audience: public
    purpose: product_story
  - path: /now
    title: What is real today
    audience: public
    purpose: proof_shelf
  - path: /horizons
    title: Coming next
    audience: public
    purpose: horizon_summary
  - path: /downloads
    title: Downloads
    audience: public
    purpose: release_shelf
  - path: /participate
    title: Participate
    audience: public
    purpose: help_and_booster_entry
  - path: /status
    title: Public status
    audience: public
    purpose: status_summary
  - path: /artifacts
    title: Featured artifacts
    audience: public
    purpose: teaser_gallery
registered_routes:
  - path: /home
    title: Home
    audience: registered
    purpose: signed_in_dashboard
  - path: /account
    title: Account
    audience: registered
    purpose: profile_and_preferences
sections:
  - id: hero
    title: Shadowrun rules truth, with receipts.
    audience: public
    route: /
  - id: what_you_can_do_today
    title: What you can do today
    audience: public
    route: /
  - id: why_this_feels_different
    title: Why this feels different
    audience: public
    route: /
  - id: choose_your_lane
    title: Choose your lane
    audience: public
    route: /
  - id: whats_real_now
    title: What is real now
    audience: public
    route: /now
  - id: coming_next
    title: Coming next
    audience: public
    route: /horizons
  - id: featured_artifacts
    title: Featured artifacts
    audience: public
    route: /artifacts
  - id: participate
    title: Participate and help
    audience: public
    route: /participate
  - id: release_shelf
    title: Download and release shelf
    audience: public
    route: /downloads
registered_overlays:
  - id: follow_horizons
    path: /home
    title: Follow horizons
    summary: Track the futures you care about without turning advisory signals into canon.
  - id: beta_interest
    path: /home
    title: Beta interest
    summary: Raise your hand for future player, GM, or creator previews.
  - id: participate_booster
    path: /participate
    title: Booster path
    summary: Signed-in users can unlock the bounded booster flow and see sponsor-session state.
  - id: vote_placeholder
    path: /home
    title: Future vote placeholder
    summary: Advisory signals may appear later, but they do not become design authority.
footer_canonical_source: chummer6-design
footer_generated_note: Generated from the canonical landing manifest, feature registry, and current public design surface.
""";

    private const string DefaultFeatureRegistryYaml = """
product: chummer
surface: chummer.run
version: 1
cards:
  - id: product_story
    bucket: what_you_can_do_today
    title: What is Chummer?
    summary: Start with the product story instead of repo history.
    href: /what-is-chummer
    badge: Read first
    audience: public
    image_family: archive_stair
  - id: see_whats_real
    bucket: what_you_can_do_today
    title: See what is real now
    summary: Check the live proof shelf before you fall in love with a promise.
    href: /now
    badge: Available today
    audience: public
    image_family: dossier_desk
  - id: get_the_poc
    bucket: what_you_can_do_today
    title: Get the POC
    summary: Grab the current drop, notes, and integrity hints.
    href: /downloads
    badge: POC
    audience: public
    image_family: facility_exterior
  - id: sign_in_follow
    bucket: what_you_can_do_today
    title: Sign in to follow the future
    summary: Keep an eye on the lanes you care about and unlock participation later.
    href: /home
    badge: Registered soon
    audience: public
    image_family: boulevard_of_futures
  - id: deterministic_truth
    bucket: why_this_feels_different
    title: Deterministic rules truth
    summary: No fuzzy math. Core-owned receipts stay attached to the answer.
    href: /what-is-chummer
    badge: Trust
    audience: public
    image_family: simulation_bench
  - id: receipts_and_provenance
    bucket: why_this_feels_different
    title: Receipts and provenance
    summary: Show the work, keep the evidence, and stop asking people to trust vibes.
    href: /now
    badge: Proof
    audience: public
    image_family: dossier_desk
  - id: local_first_continuity
    bucket: why_this_feels_different
    title: Local-first session continuity
    summary: The table should not panic just because the network does.
    href: /what-is-chummer
    badge: Continuity
    audience: public
    image_family: streetfront
  - id: lane_player
    bucket: choose_your_lane
    title: Player
    summary: Build and inspect, carry your dossier, and stay synced later.
    href: /what-is-chummer
    badge: Public now
    audience: public
    image_family: solo_operator
  - id: lane_gm
    bucket: choose_your_lane
    title: GM
    summary: Run the table, brief the team, and see what tools are coming next.
    href: /horizons
    badge: Horizon mix
    audience: public
    image_family: archive_stair
  - id: lane_creator
    bucket: choose_your_lane
    title: Creator
    summary: Publish artifacts, primers, and packs as those lanes open up.
    href: /artifacts
    badge: Coming next
    audience: public
    image_family: prop_cluster
  - id: real_rules_truth
    bucket: whats_real_now
    title: Deterministic rules truth
    summary: Core-owned math and receipts are already the trust anchor.
    href: /now
    badge: Available today
    audience: public
    image_family: simulation_bench
  - id: real_public_guide
    bucket: whats_real_now
    title: Public guide and horizons
    summary: There is already a real human guide and a governed horizon shelf.
    href: https://github.com/ArchonMegalon/Chummer6
    badge: Available today
    audience: public
    image_family: archive_stair
  - id: real_mobile_prep
    bucket: whats_real_now
    title: Local-first prep and session posture
    summary: Mobile and session continuity are treated as real product surfaces, not hand-wavy add-ons.
    href: /now
    badge: Preview
    audience: public
    image_family: streetfront
  - id: real_release_shelf
    bucket: whats_real_now
    title: Release shelf
    summary: Public drops, notes, and integrity clues live on one shelf.
    href: /downloads
    badge: POC
    audience: public
    image_family: facility_exterior
  - id: horizon_nexus_pan
    bucket: coming_next
    title: NEXUS-PAN
    summary: Shared state survives device churn without the table losing trust.
    href: /horizons#nexus-pan
    badge: Horizon
    audience: public
    image_family: streetfront
    pain: My devices drift and the table loses confidence.
    payoff: Shared state survives device churn without the GM rebuilding the run from memory.
  - id: horizon_alice
    bucket: coming_next
    title: ALICE
    summary: Builders get grounded what-if analysis without trusting fuzzy assistants.
    href: /horizons#alice
    badge: Horizon
    audience: public
    image_family: simulation_bench
    pain: We only discover weak builds after they explode in public.
    payoff: Compare grounded tradeoffs with receipts instead of vibes.
  - id: horizon_knowledge_fabric
    bucket: coming_next
    title: KNOWLEDGE FABRIC
    summary: Ask a rules question and get a grounded answer with citations and receipts.
    href: /horizons#knowledge-fabric
    badge: Horizon
    audience: public
    image_family: archive_stair
    pain: Rules answers are expensive, repetitive, and still easy to hallucinate.
    payoff: Derived projections stay grounded instead of becoming assistant folklore.
  - id: horizon_karma_forge
    bucket: coming_next
    title: KARMA FORGE
    summary: Governed rules evolution without unreadable fork chaos.
    href: /horizons#karma-forge
    badge: Booster first
    audience: public
    image_family: boulevard_of_futures
    pain: I want house rules without forking trust.
    payoff: Rule-pack changes carry approvals, compatibility, and rollback posture.
  - id: horizon_jackpoint
    bucket: coming_next
    title: JACKPOINT
    summary: Dossiers, recaps, and briefings that feel finished without severing provenance.
    href: /horizons#jackpoint
    badge: Horizon
    audience: public
    image_family: dossier_desk
    pain: I want mission packets that do not feel like a copy-paste graveyard.
    payoff: Grounded artifacts can feel polished without becoming fictional truth.
  - id: horizon_runsite
    bucket: coming_next
    title: RUNSITE
    summary: Walk the job before the team walks into it.
    href: /horizons#runsite
    badge: Horizon
    audience: public
    image_family: facility_exterior
    pain: Spatial prep is tedious and scattered.
    payoff: The venue becomes a visible, inspectable run surface.
  - id: horizon_runbook_press
    bucket: coming_next
    title: RUNBOOK PRESS
    summary: Bring new players in without the two-hour lecture.
    href: /horizons#runbook-press
    badge: Horizon
    audience: public
    image_family: prop_cluster
    pain: Table onboarding still depends on heroic patience.
    payoff: Grounded primers become first-class artifacts instead of random PDFs.
  - id: horizon_ghostwire
    bucket: coming_next
    title: GHOSTWIRE
    summary: Replay and after-action truth that survives memory drift.
    href: /horizons#ghostwire
    badge: Horizon
    audience: public
    image_family: boulevard_of_futures
    pain: People remember the drama and forget the sequence.
    payoff: After-action review can point at the event trail instead of table folklore.
  - id: horizon_local_co_processor
    bucket: coming_next
    title: LOCAL CO-PROCESSOR
    summary: Optional local acceleration without turning the product into a black box.
    href: /horizons#local-co-processor
    badge: Horizon
    audience: public
    image_family: solo_operator
    pain: Some heavy local workflows still need more muscle.
    payoff: Local acceleration stays optional and bounded instead of becoming a mystery dependency.
  - id: artifact_runsite_pack
    bucket: featured_artifacts
    title: Runsite pack
    summary: Walk the job before the team walks into it.
    href: /artifacts
    badge: Coming soon
    audience: public
    image_family: facility_exterior
  - id: artifact_dossier_brief
    bucket: featured_artifacts
    title: Dossier brief
    summary: Turn notes into a credible mission packet.
    href: /artifacts
    badge: Coming soon
    audience: public
    image_family: dossier_desk
  - id: artifact_campaign_primer
    bucket: featured_artifacts
    title: Campaign primer
    summary: Bring new players in without the two-hour lecture.
    href: /artifacts
    badge: Coming soon
    audience: public
    image_family: prop_cluster
  - id: artifact_after_action
    bucket: featured_artifacts
    title: Replay and after-action
    summary: See what happened, not just what people remember.
    href: /artifacts
    badge: Coming soon
    audience: public
    image_family: boulevard_of_futures
  - id: participate_feedback
    bucket: participate
    title: Report a problem
    summary: Use the public issue tracker when you want to file a bug or a concrete improvement.
    href: https://github.com/ArchonMegalon/Chummer6/issues
    badge: Public
    audience: public
    image_family: archive_stair
  - id: participate_future
    bucket: participate
    title: Suggest a future
    summary: Advisory signals are welcome even though canon still lives in design.
    href: /participate
    badge: Public
    audience: public
    image_family: boulevard_of_futures
  - id: participate_booster
    bucket: participate
    title: I have a ChatGPT subscription
    summary: Open the bounded booster lane if you want to lend temporary premium capacity.
    href: /participate
    badge: Registered
    audience: public
    image_family: solo_operator
  - id: participate_beta
    bucket: participate
    title: Join the beta waitlist
    summary: Raise your hand for follow-up previews and future gated surfaces.
    href: /home
    badge: Registered soon
    audience: public
    image_family: dossier_desk
  - id: release_latest
    bucket: release_shelf
    title: Latest POC build
    summary: Current public drop, release notes, and integrity clues.
    href: https://github.com/ArchonMegalon/Chummer6/releases
    badge: POC
    audience: public
    image_family: facility_exterior
""";
}
