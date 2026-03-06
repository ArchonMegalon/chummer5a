#nullable enable annotations

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Chummer.Contracts.Content;
using Chummer.Contracts.Hub;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Session;
using Chummer.Presentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class ApiIntegrationTests
{
    private static readonly Uri BaseUri = ResolveBaseUri();
    private static readonly string? ApiKey = ResolveApiKey();
    private static readonly string? ExpectedAmendId = ResolveExpectedAmendId();
    private static readonly TimeSpan HttpTimeout = ResolveHttpTimeout();
    private static readonly string[] AllSectionIds =
    {
        "attributes",
        "attributedetails",
        "inventory",
        "profile",
        "progress",
        "rules",
        "build",
        "movement",
        "awakening",
        "gear",
        "weapons",
        "weaponaccessories",
        "armors",
        "armormods",
        "cyberwares",
        "vehicles",
        "vehiclemods",
        "skills",
        "qualities",
        "contacts",
        "spells",
        "powers",
        "complexforms",
        "spirits",
        "foci",
        "aiprograms",
        "martialarts",
        "limitmodifiers",
        "lifestyles",
        "metamagics",
        "arts",
        "initiationgrades",
        "critterpowers",
        "mentorspirits",
        "expenses",
        "sources",
        "gearlocations",
        "armorlocations",
        "weaponlocations",
        "vehiclelocations",
        "calendar",
        "improvements",
        "customdatadirectorynames",
        "drugs"
    };

    [TestMethod]
    public async Task Info_endpoint_reports_chummer_service()
    {
        using var client = CreateClient();

        JsonObject info = await GetRequiredJsonObject(client, "/api/info");

        Assert.AreEqual("Chummer", info["service"]?.GetValue<string>());
        Assert.AreEqual("running", info["status"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Info_endpoint_reports_content_overlay_metadata()
    {
        using var client = CreateClient();

        JsonObject info = await GetRequiredJsonObject(client, "/api/info");

        Assert.IsInstanceOfType<JsonObject>(info["content"]);
        JsonObject content = (JsonObject)info["content"]!;
        Assert.IsNotNull(content["baseDataPath"]);
        Assert.IsNotNull(content["baseLanguagePath"]);
        Assert.IsInstanceOfType<JsonArray>(content["overlays"]);
    }

    [TestMethod]
    public async Task Content_overlays_endpoint_reports_catalog_and_expected_overlay_when_configured()
    {
        using var client = CreateClient();

        JsonObject overlays = await GetRequiredJsonObject(client, "/api/content/overlays");
        Assert.IsNotNull(overlays["baseDataPath"]);
        Assert.IsNotNull(overlays["baseLanguagePath"]);
        Assert.IsInstanceOfType<JsonArray>(overlays["overlays"]);

        if (!string.IsNullOrWhiteSpace(ExpectedAmendId))
        {
            JsonArray items = (JsonArray)overlays["overlays"]!;
            bool found = items.OfType<JsonObject>()
                .Any(item => string.Equals(item["id"]?.GetValue<string>(), ExpectedAmendId, StringComparison.Ordinal));
            Assert.IsTrue(found, $"Expected overlay id '{ExpectedAmendId}' was not found.");
        }
    }

    [TestMethod]
    public async Task Hub_search_endpoint_returns_mixed_catalog_items_for_rulepacks_profiles_and_runtime_locks()
    {
        using var client = CreateClient();
        BrowseQuery query = new(
            QueryText: string.Empty,
            FacetSelections: new Dictionary<string, IReadOnlyList<string>>(),
            SortId: "title");

        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/hub/search", query);
        response.EnsureSuccessStatusCode();
        JsonNode parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(parsed);
        JsonObject payload = (JsonObject)parsed!;

        Assert.IsNotNull(payload["totalCount"]);
        Assert.IsInstanceOfType<JsonArray>(payload["items"]);
        Assert.IsInstanceOfType<JsonArray>(payload["facets"]);
        Assert.IsInstanceOfType<JsonArray>(payload["sorts"]);
    }

    [TestMethod]
    public async Task Hub_project_detail_endpoint_returns_registered_profile_projection()
    {
        using var client = CreateClient();

        JsonObject payload = await GetRequiredJsonObject(client, "/api/hub/projects/ruleprofile/official.sr5.core?ruleset=sr5");

        Assert.AreEqual(HubCatalogItemKinds.RuleProfile, payload["summary"]?["kind"]?.GetValue<string>());
        Assert.AreEqual("official.sr5.core", payload["summary"]?["itemId"]?.GetValue<string>());
        Assert.IsNotNull(payload["runtimeFingerprint"]);
        Assert.IsInstanceOfType<JsonArray>(payload["facts"]);
        Assert.IsInstanceOfType<JsonArray>(payload["actions"]);
    }

    [TestMethod]
    public async Task Hub_project_detail_endpoint_returns_not_found_for_unknown_project()
    {
        using var client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/hub/projects/ruleprofile/missing-profile?ruleset=sr5");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        JsonNode parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(parsed);
        JsonObject payload = (JsonObject)parsed!;

        Assert.AreEqual("hub_project_not_found", payload["error"]?.GetValue<string>());
        Assert.AreEqual(HubCatalogItemKinds.RuleProfile, payload["kind"]?.GetValue<string>());
        Assert.AreEqual("missing-profile", payload["itemId"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Hub_project_install_preview_endpoint_returns_registered_profile_preview()
    {
        using var client = CreateClient();
        RuleProfileApplyTarget target = new(RuleProfileApplyTargetKinds.Workspace, "workspace-1");

        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/hub/projects/ruleprofile/official.sr5.core/install-preview?ruleset=sr5", target);
        response.EnsureSuccessStatusCode();
        JsonNode parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(parsed);
        JsonObject payload = (JsonObject)parsed!;

        Assert.AreEqual(HubCatalogItemKinds.RuleProfile, payload["kind"]?.GetValue<string>());
        Assert.AreEqual("official.sr5.core", payload["itemId"]?.GetValue<string>());
        Assert.AreEqual("ready", payload["state"]?.GetValue<string>());
        Assert.IsNotNull(payload["runtimeFingerprint"]);
        Assert.IsInstanceOfType<JsonArray>(payload["changes"]);
        Assert.IsInstanceOfType<JsonArray>(payload["diagnostics"]);
    }

    [TestMethod]
    public async Task Hub_project_install_preview_endpoint_returns_not_found_for_unknown_project()
    {
        using var client = CreateClient();
        RuleProfileApplyTarget target = new(RuleProfileApplyTargetKinds.Workspace, "workspace-1");

        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/hub/projects/ruleprofile/missing-profile/install-preview?ruleset=sr5", target);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        JsonNode parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(parsed);
        JsonObject payload = (JsonObject)parsed!;

        Assert.AreEqual("hub_project_not_found", payload["error"]?.GetValue<string>());
        Assert.AreEqual(HubCatalogItemKinds.RuleProfile, payload["kind"]?.GetValue<string>());
        Assert.AreEqual("missing-profile", payload["itemId"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Hub_project_compatibility_endpoint_returns_registered_profile_matrix()
    {
        using var client = CreateClient();

        JsonObject payload = await GetRequiredJsonObject(client, "/api/hub/projects/ruleprofile/official.sr5.core/compatibility?ruleset=sr5");

        Assert.AreEqual(HubCatalogItemKinds.RuleProfile, payload["kind"]?.GetValue<string>());
        Assert.AreEqual("official.sr5.core", payload["itemId"]?.GetValue<string>());
        Assert.IsInstanceOfType<JsonArray>(payload["rows"]);
    }

    [TestMethod]
    public async Task Hub_publish_draft_endpoint_persists_owner_draft_receipt()
    {
        using var client = CreateClient();
        string projectId = $"campaign.shadowops.{Guid.NewGuid():N}";
        HubPublishDraftRequest request = new(
            ProjectKind: HubCatalogItemKinds.RulePack,
            ProjectId: projectId,
            RulesetId: RulesetDefaults.Sr5,
            Title: "Campaign ShadowOps",
            Summary: "Street-level runtime",
            Description: "Campaign-specific SR5 publication draft.");

        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/hub/publish/drafts", request);
        response.EnsureSuccessStatusCode();
        JsonObject payload = await ParseRequiredJsonObject(response);

        Assert.IsNotNull(payload["draftId"]?.GetValue<string>());
        Assert.AreEqual(HubCatalogItemKinds.RulePack, payload["projectKind"]?.GetValue<string>());
        Assert.AreEqual(projectId, payload["projectId"]?.GetValue<string>());
        Assert.AreEqual("Street-level runtime", payload["summary"]?.GetValue<string>());
        Assert.AreEqual(HubPublicationStates.Draft, payload["state"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Hub_publish_drafts_endpoint_lists_persisted_owner_drafts()
    {
        using var client = CreateClient();
        string projectId = $"campaign.shadowops.{Guid.NewGuid():N}";

        using HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/hub/publish/drafts", new HubPublishDraftRequest(
            ProjectKind: HubCatalogItemKinds.RulePack,
            ProjectId: projectId,
            RulesetId: RulesetDefaults.Sr5,
            Title: "Campaign ShadowOps",
            Summary: "Street-level runtime",
            Description: "Campaign-specific SR5 publication draft."));
        createResponse.EnsureSuccessStatusCode();

        JsonObject payload = await GetRequiredJsonObject(client, "/api/hub/publish/drafts?kind=rulepack&ruleset=sr5");
        JsonArray items = (JsonArray)payload["items"]!;
        bool found = items.OfType<JsonObject>()
            .Any(item => string.Equals(item["projectId"]?.GetValue<string>(), projectId, StringComparison.Ordinal));

        Assert.IsTrue(found, $"Expected owner draft '{projectId}' to be listed.");
    }

    [TestMethod]
    public async Task Hub_publish_draft_detail_endpoint_returns_persisted_draft_projection()
    {
        using var client = CreateClient();
        string projectId = $"campaign.shadowops.{Guid.NewGuid():N}";

        using HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/hub/publish/drafts", new HubPublishDraftRequest(
            ProjectKind: HubCatalogItemKinds.RulePack,
            ProjectId: projectId,
            RulesetId: RulesetDefaults.Sr5,
            Title: "Campaign ShadowOps",
            Summary: "Street-level runtime",
            Description: "Campaign-specific SR5 publication draft."));
        createResponse.EnsureSuccessStatusCode();
        JsonObject created = await ParseRequiredJsonObject(createResponse);
        string draftId = created["draftId"]?.GetValue<string>() ?? string.Empty;

        JsonObject detail = await GetRequiredJsonObject(client, $"/api/hub/publish/drafts/{draftId}");

        Assert.AreEqual(draftId, detail["draft"]?["draftId"]?.GetValue<string>());
        Assert.AreEqual(projectId, detail["draft"]?["projectId"]?.GetValue<string>());
        Assert.AreEqual("Street-level runtime", detail["draft"]?["summary"]?.GetValue<string>());
        Assert.AreEqual("Campaign-specific SR5 publication draft.", detail["description"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Hub_publish_draft_update_endpoint_updates_persisted_draft_metadata()
    {
        using var client = CreateClient();
        string projectId = $"campaign.shadowops.{Guid.NewGuid():N}";

        using HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/hub/publish/drafts", new HubPublishDraftRequest(
            ProjectKind: HubCatalogItemKinds.RulePack,
            ProjectId: projectId,
            RulesetId: RulesetDefaults.Sr5,
            Title: "Campaign ShadowOps"));
        createResponse.EnsureSuccessStatusCode();
        JsonObject created = await ParseRequiredJsonObject(createResponse);
        string draftId = created["draftId"]?.GetValue<string>() ?? string.Empty;

        JsonObject updated = await PutRequiredJsonObject(client, $"/api/hub/publish/drafts/{draftId}", new JsonObject
        {
            ["title"] = "Campaign ShadowOps Updated",
            ["summary"] = "Street-level runtime",
            ["description"] = "Campaign-specific SR5 publication draft."
        });
        JsonObject detail = await GetRequiredJsonObject(client, $"/api/hub/publish/drafts/{draftId}");

        Assert.AreEqual("Campaign ShadowOps Updated", updated["title"]?.GetValue<string>());
        Assert.AreEqual("Street-level runtime", updated["summary"]?.GetValue<string>());
        Assert.AreEqual("Campaign ShadowOps Updated", detail["draft"]?["title"]?.GetValue<string>());
        Assert.AreEqual("Street-level runtime", detail["draft"]?["summary"]?.GetValue<string>());
        Assert.AreEqual("Campaign-specific SR5 publication draft.", detail["description"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Hub_publish_submit_endpoint_persists_submission_receipt_and_queue_entry()
    {
        using var client = CreateClient();
        string projectId = $"campaign.shadowops.{Guid.NewGuid():N}";
        using HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/hub/publish/drafts", new HubPublishDraftRequest(
            ProjectKind: HubCatalogItemKinds.RulePack,
            ProjectId: projectId,
            RulesetId: RulesetDefaults.Sr5,
            Title: "Campaign ShadowOps"));
        createResponse.EnsureSuccessStatusCode();
        HubSubmitProjectRequest request = new("submit for moderation");

        using HttpResponseMessage response = await client.PostAsJsonAsync($"/api/hub/publish/rulepack/{projectId}/submit?ruleset=sr5", request);
        response.EnsureSuccessStatusCode();
        JsonObject payload = await ParseRequiredJsonObject(response);

        Assert.IsNotNull(payload["draftId"]?.GetValue<string>());
        Assert.IsNotNull(payload["caseId"]?.GetValue<string>());
        Assert.AreEqual(HubCatalogItemKinds.RulePack, payload["projectKind"]?.GetValue<string>());
        Assert.AreEqual(projectId, payload["projectId"]?.GetValue<string>());
        Assert.AreEqual(HubPublicationStates.Submitted, payload["state"]?.GetValue<string>());
        Assert.AreEqual(HubModerationStates.PendingReview, payload["reviewState"]?.GetValue<string>());

        JsonObject queue = await GetRequiredJsonObject(client, "/api/hub/moderation/queue?state=pending-review");
        JsonArray items = (JsonArray)queue["items"]!;
        bool found = items.OfType<JsonObject>()
            .Any(item => string.Equals(item["projectId"]?.GetValue<string>(), projectId, StringComparison.Ordinal));
        Assert.IsTrue(found, $"Expected moderation queue to include '{projectId}'.");
    }

    [TestMethod]
    public async Task Hub_moderation_queue_endpoint_returns_queue_payload()
    {
        using var client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/hub/moderation/queue");
        response.EnsureSuccessStatusCode();
        JsonObject payload = await ParseRequiredJsonObject(response);

        Assert.IsInstanceOfType<JsonArray>(payload["items"]);
    }

    [TestMethod]
    public async Task Hub_project_compatibility_endpoint_returns_not_found_for_unknown_project()
    {
        using var client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/hub/projects/ruleprofile/missing-profile/compatibility?ruleset=sr5");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        JsonNode parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(parsed);
        JsonObject payload = (JsonObject)parsed!;

        Assert.AreEqual("hub_project_not_found", payload["error"]?.GetValue<string>());
        Assert.AreEqual(HubCatalogItemKinds.RuleProfile, payload["kind"]?.GetValue<string>());
        Assert.AreEqual("missing-profile", payload["itemId"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Buildkits_endpoint_reports_registry_entries_for_registered_rulesets()
    {
        using var client = CreateClient();

        JsonObject buildkits = await GetRequiredJsonObject(client, "/api/buildkits?ruleset=sr5");
        Assert.IsNotNull(buildkits["count"]);
        Assert.IsInstanceOfType<JsonArray>(buildkits["entries"]);
    }

    [TestMethod]
    public async Task Buildkit_detail_endpoint_returns_not_found_for_unknown_buildkit()
    {
        using var client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/buildkits/missing-buildkit?ruleset=sr5");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        JsonNode parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(parsed);
        JsonObject payload = (JsonObject)parsed!;

        Assert.AreEqual("buildkit_not_found", payload["error"]?.GetValue<string>());
        Assert.AreEqual("missing-buildkit", payload["buildKitId"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Rulepacks_endpoint_reports_registry_entries_and_expected_overlay_pack_when_configured()
    {
        using var client = CreateClient();

        JsonObject rulepacks = await GetRequiredJsonObject(client, "/api/rulepacks?ruleset=sr5");
        Assert.IsNotNull(rulepacks["count"]);
        Assert.IsInstanceOfType<JsonArray>(rulepacks["entries"]);

        if (!string.IsNullOrWhiteSpace(ExpectedAmendId))
        {
            JsonArray items = (JsonArray)rulepacks["entries"]!;
            bool found = items.OfType<JsonObject>()
                .Any(item => string.Equals(item["manifest"]?["packId"]?.GetValue<string>(), ExpectedAmendId, StringComparison.Ordinal));
            Assert.IsTrue(found, $"Expected rulepack id '{ExpectedAmendId}' was not found.");
        }
    }

    [TestMethod]
    public async Task Rulepack_detail_endpoint_returns_not_found_for_unknown_pack()
    {
        using var client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/rulepacks/missing-pack?ruleset=sr5");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        JsonNode parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(parsed);
        JsonObject payload = (JsonObject)parsed!;

        Assert.AreEqual("rulepack_not_found", payload["error"]?.GetValue<string>());
        Assert.AreEqual("missing-pack", payload["packId"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Rulepack_install_endpoints_return_not_found_for_unknown_pack()
    {
        using var client = CreateClient();
        RuleProfileApplyTarget target = new(RuleProfileApplyTargetKinds.Workspace, "workspace-1");

        using HttpResponseMessage previewResponse = await client.PostAsJsonAsync("/api/rulepacks/missing-pack/install-preview?ruleset=sr5", target);
        Assert.AreEqual(HttpStatusCode.NotFound, previewResponse.StatusCode);
        JsonNode previewParsed = JsonNode.Parse(await previewResponse.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(previewParsed);
        JsonObject previewPayload = (JsonObject)previewParsed!;
        Assert.AreEqual("rulepack_not_found", previewPayload["error"]?.GetValue<string>());

        using HttpResponseMessage installResponse = await client.PostAsJsonAsync("/api/rulepacks/missing-pack/install?ruleset=sr5", target);
        Assert.AreEqual(HttpStatusCode.NotFound, installResponse.StatusCode);
        JsonNode installParsed = JsonNode.Parse(await installResponse.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(installParsed);
        JsonObject installPayload = (JsonObject)installParsed!;
        Assert.AreEqual("rulepack_not_found", installPayload["error"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Profiles_endpoint_reports_curated_install_targets_for_registered_rulesets()
    {
        using var client = CreateClient();

        JsonObject profiles = await GetRequiredJsonObject(client, "/api/profiles?ruleset=sr5");
        Assert.IsNotNull(profiles["count"]);
        Assert.IsInstanceOfType<JsonArray>(profiles["entries"]);

        JsonArray items = (JsonArray)profiles["entries"]!;
        bool found = items.OfType<JsonObject>()
            .Any(item => string.Equals(item["manifest"]?["profileId"]?.GetValue<string>(), "official.sr5.core", StringComparison.Ordinal));
        Assert.IsTrue(found, "Expected default RuleProfile id 'official.sr5.core' was not found.");
    }

    [TestMethod]
    public async Task Profile_detail_endpoint_returns_not_found_for_unknown_profile()
    {
        using var client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/profiles/missing-profile?ruleset=sr5");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        JsonNode parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(parsed);
        JsonObject payload = (JsonObject)parsed!;

        Assert.AreEqual("ruleprofile_not_found", payload["error"]?.GetValue<string>());
        Assert.AreEqual("missing-profile", payload["profileId"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Profile_preview_endpoint_returns_runtime_lock_preview_for_registered_profile()
    {
        using var client = CreateClient();
        RuleProfileApplyTarget target = new(RuleProfileApplyTargetKinds.Workspace, "workspace-1");

        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/profiles/official.sr5.core/preview?ruleset=sr5", target);
        response.EnsureSuccessStatusCode();
        JsonNode parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(parsed);
        JsonObject payload = (JsonObject)parsed!;

        Assert.AreEqual("official.sr5.core", payload["profileId"]?.GetValue<string>());
        Assert.AreEqual("workspace-1", payload["target"]?["targetId"]?.GetValue<string>());
        Assert.IsNotNull(payload["runtimeLock"]?["runtimeFingerprint"]);
    }

    [TestMethod]
    public async Task Profile_apply_endpoint_returns_applied_receipt_for_registered_profile()
    {
        using var client = CreateClient();
        RuleProfileApplyTarget target = new(RuleProfileApplyTargetKinds.Character, "character-1");

        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/profiles/official.sr5.core/apply?ruleset=sr5", target);
        response.EnsureSuccessStatusCode();
        JsonNode parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(parsed);
        JsonObject payload = (JsonObject)parsed!;

        Assert.AreEqual(RuleProfileApplyOutcomes.Applied, payload["outcome"]?.GetValue<string>());
        Assert.IsNull(payload["deferredReason"]);
        Assert.AreEqual("character-1", payload["target"]?["targetId"]?.GetValue<string>());
        Assert.AreEqual("character-1", payload["installReceipt"]?["targetId"]?.GetValue<string>());
        Assert.IsNotNull(payload["installReceipt"]?["runtimeLock"]?["runtimeFingerprint"]);
    }

    [TestMethod]
    public async Task Runtime_profile_endpoint_returns_runtime_inspector_projection_for_registered_profile()
    {
        using var client = CreateClient();

        JsonObject payload = await GetRequiredJsonObject(client, "/api/runtime/profiles/official.sr5.core?ruleset=sr5");

        Assert.AreEqual("runtime-lock", payload["targetKind"]?.GetValue<string>());
        Assert.AreEqual("official.sr5.core", payload["targetId"]?.GetValue<string>());
        Assert.IsInstanceOfType<JsonObject>(payload["runtimeLock"]);
        Assert.IsInstanceOfType<JsonArray>(payload["resolvedRulePacks"]);
        Assert.IsInstanceOfType<JsonArray>(payload["compatibilityDiagnostics"]);
    }

    [TestMethod]
    public async Task Runtime_locks_endpoint_returns_runtime_lock_catalog_for_registered_profiles()
    {
        using var client = CreateClient();

        JsonObject payload = await GetRequiredJsonObject(client, "/api/runtime/locks?ruleset=sr5");

        Assert.IsNotNull(payload["count"]);
        Assert.IsInstanceOfType<JsonArray>(payload["entries"]);
    }

    [TestMethod]
    public async Task Runtime_lock_detail_endpoint_returns_not_found_for_unknown_lock()
    {
        using var client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/runtime/locks/missing-lock?ruleset=sr5");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        JsonNode parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(parsed);
        JsonObject payload = (JsonObject)parsed!;

        Assert.AreEqual("runtime_lock_not_found", payload["error"]?.GetValue<string>());
        Assert.AreEqual("missing-lock", payload["lockId"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Runtime_lock_install_endpoints_preview_and_persist_owner_install_state()
    {
        using var client = CreateClient();
        JsonObject runtimeLocks = await GetRequiredJsonObject(client, "/api/runtime/locks?ruleset=sr5");
        JsonArray items = (JsonArray)runtimeLocks["entries"]!;
        JsonObject first = items.OfType<JsonObject>().First();
        string lockId = first["lockId"]!.GetValue<string>();
        RuleProfileApplyTarget target = new(RuleProfileApplyTargetKinds.Workspace, "workspace-1");

        using HttpResponseMessage previewResponse = await client.PostAsJsonAsync($"/api/runtime/locks/{lockId}/install-preview?ruleset=sr5", target);
        previewResponse.EnsureSuccessStatusCode();
        JsonNode previewParsed = JsonNode.Parse(await previewResponse.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(previewParsed);
        JsonObject previewPayload = (JsonObject)previewParsed!;
        Assert.AreEqual(lockId, previewPayload["lockId"]?.GetValue<string>());
        Assert.IsInstanceOfType<JsonArray>(previewPayload["changes"]);

        using HttpResponseMessage installResponse = await client.PostAsJsonAsync($"/api/runtime/locks/{lockId}/install?ruleset=sr5", target);
        installResponse.EnsureSuccessStatusCode();
        JsonNode installParsed = JsonNode.Parse(await installResponse.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(installParsed);
        JsonObject installPayload = (JsonObject)installParsed!;
        string? outcome = installPayload["outcome"]?.GetValue<string>();
        Assert.IsTrue(
            string.Equals(outcome, RuntimeLockInstallOutcomes.Installed, StringComparison.Ordinal)
            || string.Equals(outcome, RuntimeLockInstallOutcomes.Updated, StringComparison.Ordinal)
            || string.Equals(outcome, RuntimeLockInstallOutcomes.Unchanged, StringComparison.Ordinal),
            $"Unexpected runtime lock install outcome '{outcome}'.");

        JsonObject detailPayload = await GetRequiredJsonObject(client, $"/api/runtime/locks/{lockId}?ruleset=sr5");
        Assert.AreEqual(RuntimeLockCatalogKinds.Saved, detailPayload["catalogKind"]?.GetValue<string>());
        Assert.AreEqual(ArtifactInstallStates.Pinned, detailPayload["install"]?["state"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Health_endpoint_reports_ok()
    {
        using var client = CreateClient();

        JsonObject health = await GetRequiredJsonObject(client, "/api/health");

        Assert.IsTrue(health["ok"]?.GetValue<bool>() ?? false);
    }

    [TestMethod]
    public async Task Root_endpoint_reports_api_service_document()
    {
        using var client = CreateClient();

        JsonObject payload = await GetRequiredJsonObject(client, "/");

        Assert.AreEqual("Chummer.Api", payload["service"]?.GetValue<string>());
        Assert.AreEqual("running", payload["status"]?.GetValue<string>());
        Assert.IsTrue(payload["docs"] is JsonArray);
    }

    [TestMethod]
    public async Task Public_endpoints_remain_accessible_without_api_key_header_when_auth_is_enabled()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            return;

        using var client = CreateClient(includeApiKey: false);

        JsonObject health = await GetRequiredJsonObject(client, "/api/health");
        Assert.IsTrue(health["ok"]?.GetValue<bool>() ?? false);

        JsonObject info = await GetRequiredJsonObject(client, "/api/info");
        Assert.AreEqual("Chummer", info["service"]?.GetValue<string>());

        BrowseQuery hubQuery = new(
            QueryText: string.Empty,
            FacetSelections: new Dictionary<string, IReadOnlyList<string>>(),
            SortId: "title");
        using HttpResponseMessage hubResponse = await client.PostAsJsonAsync("/api/hub/search", hubQuery);
        hubResponse.EnsureSuccessStatusCode();

        JsonObject rulepacks = await GetRequiredJsonObject(client, "/api/rulepacks?ruleset=sr5");
        Assert.IsNotNull(rulepacks["count"]);

        JsonObject profiles = await GetRequiredJsonObject(client, "/api/profiles?ruleset=sr5");
        Assert.IsNotNull(profiles["count"]);

        RuleProfileApplyTarget target = new(RuleProfileApplyTargetKinds.Workspace, "workspace-1");
        using HttpResponseMessage previewResponse = await client.PostAsJsonAsync("/api/profiles/official.sr5.core/preview?ruleset=sr5", target);
        previewResponse.EnsureSuccessStatusCode();

        JsonObject runtime = await GetRequiredJsonObject(client, "/api/runtime/profiles/official.sr5.core?ruleset=sr5");
        Assert.AreEqual("official.sr5.core", runtime["targetId"]?.GetValue<string>());

        JsonObject runtimeLocks = await GetRequiredJsonObject(client, "/api/runtime/locks?ruleset=sr5");
        Assert.IsNotNull(runtimeLocks["count"]);

        using HttpResponseMessage hubPreviewResponse = await client.PostAsJsonAsync(
            "/api/hub/projects/ruleprofile/official.sr5.core/install-preview?ruleset=sr5",
            target);
        hubPreviewResponse.EnsureSuccessStatusCode();
    }

    [TestMethod]
    public async Task Session_http_client_uses_explicit_not_implemented_boundary_for_character_list()
    {
        using var http = CreateClient();
        HttpSessionClient sessionClient = new(http);

        SessionApiResult<SessionCharacterCatalog> result = await sessionClient.ListCharactersAsync(default);

        Assert.IsFalse(result.IsImplemented);
        Assert.IsNotNull(result.NotImplemented);
        Assert.AreEqual("session_not_implemented", result.NotImplemented.Error);
        Assert.AreEqual(SessionApiOperations.ListCharacters, result.NotImplemented.Operation);
    }

    [TestMethod]
    public async Task Session_http_client_uses_explicit_not_implemented_boundary_for_sync()
    {
        using var http = CreateClient();
        HttpSessionClient sessionClient = new(http);
        SessionSyncBatch batch = new(
            OverlayId: "overlay-1",
            BaseCharacterVersion: new("char-1", "ver-1", "sr5", "runtime-1"),
            Events: [],
            ClientCursor: "cursor-1");

        SessionApiResult<SessionSyncReceipt> result = await sessionClient.SyncCharacterLedgerAsync("char-1", batch, default);

        Assert.IsFalse(result.IsImplemented);
        Assert.IsNotNull(result.NotImplemented);
        Assert.AreEqual("session_not_implemented", result.NotImplemented.Error);
        Assert.AreEqual(SessionApiOperations.SyncCharacterLedger, result.NotImplemented.Operation);
        Assert.AreEqual("char-1", result.NotImplemented.CharacterId);
    }

    [TestMethod]
    public async Task Protected_endpoint_requires_valid_api_key_when_auth_is_enabled()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            return;

        using var client = CreateClient(includeApiKey: false);

        using HttpResponseMessage response = await client.GetAsync("/api/tools/master-index");
        string content = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(401, (int)response.StatusCode, content);
        JsonNode? parsed = JsonNode.Parse(content);
        Assert.AreEqual("missing_or_invalid_api_key", parsed?["error"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Profile_apply_endpoint_requires_valid_api_key_when_auth_is_enabled()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            return;

        using var client = CreateClient(includeApiKey: false);
        RuleProfileApplyTarget target = new(RuleProfileApplyTargetKinds.Character, "character-1");

        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/profiles/official.sr5.core/apply?ruleset=sr5", target);
        string content = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(401, (int)response.StatusCode, content);
        JsonNode? parsed = JsonNode.Parse(content);
        Assert.AreEqual("missing_or_invalid_api_key", parsed?["error"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Rulepack_and_runtime_lock_install_endpoints_require_valid_api_key_when_auth_is_enabled()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            return;

        using var client = CreateClient(includeApiKey: false);
        RuleProfileApplyTarget target = new(RuleProfileApplyTargetKinds.Workspace, "workspace-1");

        using HttpResponseMessage rulePackPreviewResponse = await client.PostAsJsonAsync("/api/rulepacks/missing-pack/install-preview?ruleset=sr5", target);
        using HttpResponseMessage rulePackInstallResponse = await client.PostAsJsonAsync("/api/rulepacks/missing-pack/install?ruleset=sr5", target);
        using HttpResponseMessage runtimePreviewResponse = await client.PostAsJsonAsync("/api/runtime/locks/missing-lock/install-preview?ruleset=sr5", target);
        using HttpResponseMessage runtimeInstallResponse = await client.PostAsJsonAsync("/api/runtime/locks/missing-lock/install?ruleset=sr5", target);

        Assert.AreEqual(HttpStatusCode.Unauthorized, rulePackPreviewResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, rulePackInstallResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, runtimePreviewResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, runtimeInstallResponse.StatusCode);
    }

    [TestMethod]
    public async Task Contacts_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><contacts><contact><name>A</name><role>B</role><location>C</location><connection>3</connection><loyalty>2</loyalty></contact></contacts></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/contacts", body);
        Assert.AreEqual(1, response["count"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Attribute_details_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><attributes><attribute><name>BOD</name><metatypemin>1</metatypemin><metatypemax>6</metatypemax><metatypeaugmax>9</metatypeaugmax><base>3</base><karma>1</karma><totalvalue>4</totalvalue><metatypecategory>Standard</metatypecategory></attribute></attributes></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/attributedetails", body);
        Assert.AreEqual(1, response["count"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Vehicles_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><vehicles><vehicle><guid>v1</guid><name>Roadmaster</name><category>Truck</category><handling>3</handling><speed>4</speed><body>18</body><armor>16</armor><sensor>3</sensor><seats>6</seats><cost>120000</cost><mods><mod><name>GridLink Override</name></mod></mods><weapons><weapon><name>LMG</name></weapon></weapons></vehicle></vehicles></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/vehicles", body);
        Assert.AreEqual(1, response["count"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Profile_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><name>Neo</name><alias>The One</alias><playername>T</playername><metatype>Human</metatype><sex>Male</sex><age>29</age><buildmethod>Priority</buildmethod><created>True</created><adept>False</adept><magician>True</magician><technomancer>False</technomancer><ai>False</ai><mainmugshotindex>0</mainmugshotindex><mugshots><mugshot>a</mugshot></mugshots></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/profile", body);
        Assert.AreEqual("Neo", response["name"]?.GetValue<string>());
        Assert.AreEqual("Human", response["metatype"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Progress_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><karma>15</karma><nuyen>2500</nuyen><startingnuyen>6000</startingnuyen><streetcred>2</streetcred><notoriety>1</notoriety><publicawareness>0</publicawareness><burntstreetcred>0</burntstreetcred><buildkarma>25</buildkarma><totalattributes>18</totalattributes><totalspecial>2</totalspecial><physicalcmfilled>1</physicalcmfilled><stuncmfilled>3</stuncmfilled><totaless>5.25</totaless><initiategrade>0</initiategrade><submersiongrade>0</submersiongrade><magenabled>True</magenabled><resenabled>False</resenabled><depenabled>False</depenabled></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/progress", body);
        Assert.AreEqual(15m, response["karma"]?.GetValue<decimal>());
        Assert.AreEqual(2500m, response["nuyen"]?.GetValue<decimal>());
    }

    [TestMethod]
    public async Task Rules_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><gameedition>SR5</gameedition><settings>default.xml</settings><gameplayoption>Standard</gameplayoption><gameplayoptionqualitylimit>25</gameplayoptionqualitylimit><maxnuyen>10</maxnuyen><maxkarma>25</maxkarma><contactmultiplier>3</contactmultiplier><bannedwaregrades><grade>Betaware</grade><grade>Deltaware</grade></bannedwaregrades></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/rules", body);
        Assert.AreEqual("SR5", response["gameEdition"]?.GetValue<string>());
        Assert.AreEqual(2, response["bannedWareGrades"]?.AsArray().Count);
    }

    [TestMethod]
    public async Task Build_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><buildmethod>SumtoTen</buildmethod><prioritymetatype>C,2</prioritymetatype><priorityattributes>E,0</priorityattributes><priorityspecial>A,4</priorityspecial><priorityskills>B,3</priorityskills><priorityresources>D,1</priorityresources><prioritytalent>Mundane</prioritytalent><sumtoten>10</sumtoten><special>1</special><totalspecial>4</totalspecial><totalattributes>20</totalattributes><contactpoints>15</contactpoints><contactpointsused>8</contactpointsused></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/build", body);
        Assert.AreEqual("SumtoTen", response["buildMethod"]?.GetValue<string>());
        Assert.AreEqual(10, response["sumToTen"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Movement_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><walk>2/1/0</walk><run>4/0/0</run><sprint>2/1/0</sprint><walkalt>2/1/0</walkalt><runalt>4/0/0</runalt><sprintalt>2/1/0</sprintalt><physicalcmfilled>1</physicalcmfilled><stuncmfilled>3</stuncmfilled></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/movement", body);
        Assert.AreEqual("2/1/0", response["walk"]?.GetValue<string>());
        Assert.AreEqual(3, response["stunCmFilled"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Weapon_accessories_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><weapons><weapon><guid>w1</guid><name>Ares Predator</name><accessories><accessory><guid>a1</guid><name>Smartgun System</name><mount>Internal</mount><extramount>None</extramount><rating>0</rating><cost>500</cost><equipped>True</equipped></accessory></accessories></weapon></weapons></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/weaponaccessories", body);
        Assert.AreEqual(1, response["count"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Armor_mods_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><armors><armor><guid>ar1</guid><name>Armor Jacket</name><armormods><armormod><guid>m1</guid><name>Nonconductivity</name><category>General</category><rating>6</rating><cost>6000</cost><equipped>True</equipped></armormod></armormods></armor></armors></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/armormods", body);
        Assert.AreEqual(1, response["count"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Vehicle_mods_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><vehicles><vehicle><guid>v1</guid><name>Roadmaster</name><mods><mod><guid>vm1</guid><name>GridLink Override</name><category>Electromagnetic</category><slots>1</slots><rating>0</rating><cost>1000</cost><equipped>True</equipped></mod></mods></vehicle></vehicles></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/vehiclemods", body);
        Assert.AreEqual(1, response["count"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Awakening_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><magenabled>True</magenabled><resenabled>False</resenabled><depenabled>False</depenabled><adept>False</adept><magician>True</magician><technomancer>False</technomancer><ai>False</ai><initiategrade>2</initiategrade><submersiongrade>0</submersiongrade><tradition>Hermetic</tradition><traditionname>Hermetic</traditionname><traditiondrain>LOG + WIL</traditiondrain><spiritcombat>Fire</spiritcombat><spiritdetection>Air</spiritdetection><spirithealth>Water</spirithealth><spiritillusion>Earth</spiritillusion><spiritmanipulation>Man</spiritmanipulation><stream></stream><streamdrain></streamdrain><currentcounterspellingdice>3</currentcounterspellingdice><spelllimit>12</spelllimit><cfplimit>0</cfplimit><ainormalprogramlimit>0</ainormalprogramlimit><aiadvancedprogramlimit>0</aiadvancedprogramlimit></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/awakening", body);
        Assert.IsTrue(response["magEnabled"]?.GetValue<bool>() ?? false);
        Assert.AreEqual(2, response["initiateGrade"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Dice_roll_endpoint_returns_rolls()
    {
        using var client = CreateClient();

        JsonObject body = new()
        {
            ["expression"] = "8d6+2"
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/tools/dice/roll", body);
        Assert.AreEqual(8, response["rolls"]?.AsArray().Count);
        Assert.IsGreaterThanOrEqualTo(10, response["total"]?.GetValue<int>() ?? 0);
    }

    [TestMethod]
    public async Task Data_export_endpoint_returns_bundle()
    {
        using var client = CreateClient();

        const string xml = "<character><name>Neo</name><alias>The One</alias><metatype>Human</metatype><buildmethod>Priority</buildmethod><karma>15</karma><nuyen>2500</nuyen><attributes><attribute><name>BOD</name><base>3</base><karma>1</karma><metatypecategory>Standard</metatypecategory><totalvalue>4</totalvalue></attribute></attributes><skills><skill><name>Pistols</name></skill></skills><contacts><contact><name>Fixer</name></contact></contacts></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/tools/data-export", body);
        Assert.IsNotNull(response["summary"]);
        Assert.IsNotNull(response["profile"]);
        Assert.IsNotNull(response["attributes"]);
    }

    [TestMethod]
    public async Task Master_index_endpoint_returns_data()
    {
        using var client = CreateClient();

        JsonObject response = await GetRequiredJsonObject(client, "/api/tools/master-index");
        Assert.IsGreaterThan(0, response["count"]?.GetValue<int>() ?? 0);
        Assert.IsTrue(response["files"] is JsonArray);
    }

    [TestMethod]
    public async Task Translator_languages_endpoint_returns_data()
    {
        using var client = CreateClient();

        JsonObject response = await GetRequiredJsonObject(client, "/api/tools/translator/languages");
        Assert.IsGreaterThan(0, response["count"]?.GetValue<int>() ?? 0);
        Assert.IsTrue(response["languages"] is JsonArray);
    }

    [TestMethod]
    public async Task Settings_endpoints_roundtrip()
    {
        using var client = CreateClient();

        JsonObject saveBody = new()
        {
            ["uiScale"] = 110,
            ["theme"] = "classic"
        };

        JsonObject saveResponse = await PostRequiredJsonObject(client, "/api/tools/settings/global", saveBody);
        Assert.IsTrue(saveResponse["saved"]?.GetValue<bool>() ?? false);

        JsonObject getResponse = await GetRequiredJsonObject(client, "/api/tools/settings/global");
        Assert.IsNotNull(getResponse["settings"]);
    }

    [TestMethod]
    public async Task Roster_endpoints_accept_entry()
    {
        using var client = CreateClient();

        JsonObject body = new()
        {
            ["name"] = "BLUE",
            ["alias"] = "Troy",
            ["metatype"] = "Ork",
            ["lastOpenedUtc"] = DateTimeOffset.UtcNow.ToString("O")
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/tools/roster", body);
        Assert.IsGreaterThan(0, response["count"]?.GetValue<int>() ?? 0);
        Assert.IsTrue(response["entries"] is JsonArray);
    }

    [TestMethod]
    public async Task Life_modules_stages_endpoint_returns_data()
    {
        using var client = CreateClient();

        JsonNode stages = await client.GetFromJsonAsync<JsonNode>("/api/lifemodules/stages");
        Assert.IsNotNull(stages);
        Assert.IsInstanceOfType<JsonArray>(stages);
        Assert.IsGreaterThan(0, ((JsonArray)stages).Count);
    }

    [TestMethod]
    public async Task Commands_endpoint_returns_catalog()
    {
        using var client = CreateClient();

        JsonObject response = await GetRequiredJsonObject(client, "/api/commands?ruleset=sr5");
        JsonObject defaultResponse = await GetRequiredJsonObject(client, "/api/commands");

        Assert.IsGreaterThan(0, response["count"]?.GetValue<int>() ?? 0);
        Assert.IsTrue(response["commands"] is JsonArray);
        Assert.AreEqual(response.ToJsonString(), defaultResponse.ToJsonString());
    }

    [TestMethod]
    public async Task Commands_endpoint_returns_empty_catalog_for_unknown_ruleset()
    {
        using var client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/commands?ruleset=shadowrun-x");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        JsonNode parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(parsed);
        JsonObject payload = (JsonObject)parsed!;

        Assert.AreEqual("unknown_ruleset", payload["error"]?.GetValue<string>());
        Assert.AreEqual("shadowrun-x", payload["rulesetId"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Navigation_tabs_endpoint_returns_catalog()
    {
        using var client = CreateClient();

        JsonObject response = await GetRequiredJsonObject(client, "/api/navigation-tabs?ruleset=sr5");
        JsonObject defaultResponse = await GetRequiredJsonObject(client, "/api/navigation-tabs");

        Assert.IsGreaterThanOrEqualTo(16, response["count"]?.GetValue<int>() ?? 0);
        Assert.IsTrue(response["tabs"] is JsonArray);
        Assert.IsTrue((response["tabs"] as JsonArray)?.Any(node => string.Equals(node?["id"]?.GetValue<string>(), "tab-info", StringComparison.Ordinal)) ?? false);
        Assert.IsTrue((response["tabs"] as JsonArray)?.All(node => !string.IsNullOrWhiteSpace(node?["sectionId"]?.GetValue<string>())) ?? false);
        Assert.AreEqual(response.ToJsonString(), defaultResponse.ToJsonString());
    }

    [TestMethod]
    public async Task Navigation_tabs_endpoint_returns_empty_catalog_for_unknown_ruleset()
    {
        using var client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/navigation-tabs?ruleset=shadowrun-x");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        JsonNode parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(parsed);
        JsonObject payload = (JsonObject)parsed!;

        Assert.AreEqual("unknown_ruleset", payload["error"]?.GetValue<string>());
        Assert.AreEqual("shadowrun-x", payload["rulesetId"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Session_characters_endpoint_returns_not_implemented_receipt()
    {
        using var client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/session/characters");
        Assert.AreEqual(HttpStatusCode.NotImplemented, response.StatusCode);
        JsonNode parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(parsed);
        JsonObject payload = (JsonObject)parsed!;

        Assert.AreEqual("session_not_implemented", payload["error"]?.GetValue<string>());
        Assert.AreEqual("list-characters", payload["operation"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Session_character_sync_endpoint_returns_not_implemented_receipt()
    {
        using var client = CreateClient();

        using var request = new StringContent("{}", Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync("/api/session/characters/char-1/sync", request);
        Assert.AreEqual(HttpStatusCode.NotImplemented, response.StatusCode);
        JsonNode parsed = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsInstanceOfType<JsonObject>(parsed);
        JsonObject payload = (JsonObject)parsed!;

        Assert.AreEqual("session_not_implemented", payload["error"]?.GetValue<string>());
        Assert.AreEqual("sync-character-ledger", payload["operation"]?.GetValue<string>());
        Assert.AreEqual("char-1", payload["characterId"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Shell_bootstrap_endpoint_returns_ruleset_catalog_and_workspace_snapshot()
    {
        using var client = CreateClient();
        client.Timeout = TimeSpan.FromSeconds(180);
        await ClearAllWorkspacesAsync(client);
        await PostRequiredJsonObject(client, "/api/shell/preferences", new JsonObject
        {
            ["preferredRulesetId"] = "sr5"
        });

        JsonObject response = await GetRequiredJsonObject(client, "/api/shell/bootstrap?ruleset=sr5");

        Assert.AreEqual("sr5", (response["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());
        Assert.AreEqual("sr5", (response["preferredRulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());
        Assert.AreEqual("sr5", (response["activeRulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());
        Assert.IsNull(response["activeWorkspaceId"]);
        Assert.AreEqual("official.sr5.core", response["activeRuntime"]?["profileId"]?.GetValue<string>());
        Assert.IsNotNull(response["activeRuntime"]?["runtimeFingerprint"]);
        Assert.IsTrue(response["commands"] is JsonArray commands && commands.Count > 0);
        Assert.IsTrue(response["navigationTabs"] is JsonArray tabs && tabs.Count > 0);
        Assert.IsTrue(response["workflowDefinitions"] is JsonArray workflowDefinitions && workflowDefinitions.Count > 0);
        Assert.IsTrue(response["workflowSurfaces"] is JsonArray workflowSurfaces && workflowSurfaces.Count > 0);
        Assert.IsTrue(response["workspaces"] is JsonArray);
    }

    [TestMethod]
    public async Task Shell_bootstrap_endpoint_uses_preferred_ruleset_when_no_active_workspace_is_saved_even_if_workspaces_exist()
    {
        using var client = CreateClient();
        client.Timeout = TimeSpan.FromSeconds(180);
        await ClearAllWorkspacesAsync(client);
        await PostRequiredJsonObject(client, "/api/shell/preferences", new JsonObject
        {
            ["preferredRulesetId"] = "sr5"
        });

        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        JsonObject importBody = new()
        {
            ["xml"] = xml,
            ["rulesetId"] = "SR6"
        };

        await PostRequiredJsonObject(client, "/api/workspaces/import", importBody);
        JsonObject response = await GetRequiredJsonObject(client, "/api/shell/bootstrap");

        Assert.AreEqual("sr5", (response["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());
        Assert.AreEqual("sr5", (response["preferredRulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());
        Assert.AreEqual("sr5", (response["activeRulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());
        Assert.IsNull(response["activeWorkspaceId"]);
    }

    [TestMethod]
    public async Task Shell_session_endpoint_roundtrips_active_workspace_selection()
    {
        using var client = CreateClient();
        client.Timeout = TimeSpan.FromSeconds(180);

        await PostRequiredJsonObject(client, "/api/shell/session", new JsonObject
        {
            ["activeWorkspaceId"] = "ws-test",
            ["activeTabId"] = "tab-rules",
            ["activeTabsByWorkspace"] = new JsonObject
            {
                ["ws-test"] = "tab-rules"
            }
        });

        JsonObject response = await GetRequiredJsonObject(client, "/api/shell/session");
        Assert.AreEqual("ws-test", response["activeWorkspaceId"]?.GetValue<string>());
        Assert.AreEqual("tab-rules", response["activeTabId"]?.GetValue<string>());
        Assert.AreEqual("tab-rules", response["activeTabsByWorkspace"]?["ws-test"]?.GetValue<string>());

        await PostRequiredJsonObject(client, "/api/shell/session", new JsonObject());
        JsonObject cleared = await GetRequiredJsonObject(client, "/api/shell/session");
        Assert.IsNull(cleared["activeWorkspaceId"]);
        Assert.IsNull(cleared["activeTabId"]);
        Assert.IsNull(cleared["activeTabsByWorkspace"]);
    }

    [TestMethod]
    public async Task Shell_bootstrap_endpoint_uses_saved_preferred_ruleset_when_no_workspace_is_open()
    {
        using var client = CreateClient();
        client.Timeout = TimeSpan.FromSeconds(180);

        await ClearAllWorkspacesAsync(client);
        await PostRequiredJsonObject(client, "/api/shell/preferences", new JsonObject
        {
            ["preferredRulesetId"] = "sr6"
        });

        JsonObject response = await GetRequiredJsonObject(client, "/api/shell/bootstrap");

        Assert.AreEqual("sr6", (response["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());
        Assert.IsNull(response["activeWorkspaceId"]);
    }

    [TestMethod]
    public async Task Shell_bootstrap_endpoint_restores_saved_active_workspace_when_present()
    {
        using var client = CreateClient();
        client.Timeout = TimeSpan.FromSeconds(180);
        await ClearAllWorkspacesAsync(client);

        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        JsonObject sr5Import = await PostRequiredJsonObject(client, "/api/workspaces/import", new JsonObject
        {
            ["xml"] = xml,
            ["rulesetId"] = "sr5"
        });
        string sr5WorkspaceId = sr5Import["id"]?.GetValue<string>() ?? string.Empty;
        Assert.IsFalse(string.IsNullOrWhiteSpace(sr5WorkspaceId));

        await PostRequiredJsonObject(client, "/api/workspaces/import", new JsonObject
        {
            ["xml"] = xml,
            ["rulesetId"] = "sr6"
        });

        await PostRequiredJsonObject(client, "/api/shell/preferences", new JsonObject
        {
            ["preferredRulesetId"] = "sr6"
        });
        await PostRequiredJsonObject(client, "/api/shell/session", new JsonObject
        {
            ["activeWorkspaceId"] = sr5WorkspaceId,
            ["activeTabId"] = "tab-rules",
            ["activeTabsByWorkspace"] = new JsonObject
            {
                [sr5WorkspaceId] = "tab-rules"
            }
        });

        JsonObject response = await GetRequiredJsonObject(client, "/api/shell/bootstrap");

        Assert.AreEqual(sr5WorkspaceId, response["activeWorkspaceId"]?.GetValue<string>());
        Assert.AreEqual("tab-rules", response["activeTabId"]?.GetValue<string>());
        Assert.AreEqual("tab-rules", response["activeTabsByWorkspace"]?[sr5WorkspaceId]?.GetValue<string>());
        Assert.AreEqual("sr6", (response["preferredRulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());
        Assert.AreEqual("sr5", (response["activeRulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());
        Assert.AreEqual("sr5", (response["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());
    }

    [TestMethod]
    public async Task Workspace_endpoints_import_read_update_and_save_character()
    {
        using var client = CreateClient();

        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        JsonObject importBody = new()
        {
            ["xml"] = xml,
            ["rulesetId"] = "sr5"
        };

        JsonObject importResponse = await PostRequiredJsonObject(client, "/api/workspaces/import", importBody);
        string workspaceId = importResponse["id"]?.GetValue<string>() ?? string.Empty;
        Assert.IsFalse(string.IsNullOrWhiteSpace(workspaceId));
        Assert.AreEqual("sr5", (importResponse["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());

        JsonObject summary = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/summary");
        Assert.AreEqual("Cerri", summary["name"]?.GetValue<string>());
        Assert.AreEqual("Apex", summary["alias"]?.GetValue<string>());

        JsonObject validation = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/validate");
        Assert.IsTrue(validation["isValid"]?.GetValue<bool>() ?? false);
        Assert.IsTrue(validation["issues"] is JsonArray);

        JsonObject profile = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/profile");
        Assert.AreEqual("Cerri", profile["name"]?.GetValue<string>());
        Assert.AreEqual("Apex", profile["alias"]?.GetValue<string>());

        JsonObject skills = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/skills");
        Assert.IsGreaterThan(0, skills["count"]?.GetValue<int>() ?? 0);

        JsonObject rules = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/rules");
        Assert.IsFalse(string.IsNullOrWhiteSpace(rules["gameEdition"]?.GetValue<string>()));

        JsonObject build = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/build");
        Assert.AreEqual("SumtoTen", build["buildMethod"]?.GetValue<string>());

        JsonObject movement = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/movement");
        Assert.IsFalse(string.IsNullOrWhiteSpace(movement["walk"]?.GetValue<string>()));

        JsonObject awakening = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/awakening");
        Assert.IsNotNull(awakening["magEnabled"]);

        JsonObject patchBody = new()
        {
            ["name"] = "Updated Name",
            ["alias"] = "Updated Alias",
            ["notes"] = "Updated notes"
        };

        JsonObject patchResponse = await PatchRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/metadata", patchBody);
        Assert.AreEqual("Updated Name", patchResponse["profile"]?["name"]?.GetValue<string>());

        JsonObject saveResponse = await PostRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/save", new JsonObject());
        Assert.AreEqual(workspaceId, saveResponse["id"]?.GetValue<string>());
        Assert.IsGreaterThan(0, saveResponse["documentLength"]?.GetValue<int>() ?? 0);
        Assert.AreEqual("sr5", (saveResponse["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());

        JsonObject downloadResponse = await PostRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/download", new JsonObject());
        Assert.AreEqual(workspaceId, downloadResponse["id"]?.GetValue<string>());
        Assert.AreEqual("NativeXml", downloadResponse["format"]?.GetValue<string>());
        Assert.AreEqual("sr5", (downloadResponse["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());
        Assert.IsTrue((downloadResponse["fileName"]?.GetValue<string>() ?? string.Empty).EndsWith(".chum5", StringComparison.Ordinal));
        string contentBase64 = downloadResponse["contentBase64"]?.GetValue<string>() ?? string.Empty;
        Assert.IsFalse(string.IsNullOrWhiteSpace(contentBase64));
        Assert.IsGreaterThan(0, Convert.FromBase64String(contentBase64).Length);
    }

    [TestMethod]
    public async Task Workspace_import_accepts_content_base64_payload_with_utf8_bom()
    {
        using var client = CreateClient();

        byte[] xmlBytes = File.ReadAllBytes(FindTestFilePath("BLUE.chum5"));
        JsonObject importBody = new()
        {
            ["contentBase64"] = Convert.ToBase64String(xmlBytes),
            ["format"] = "NativeXml",
            ["rulesetId"] = "sr5"
        };

        JsonObject importResponse = await PostRequiredJsonObject(client, "/api/workspaces/import", importBody);
        string workspaceId = importResponse["id"]?.GetValue<string>() ?? string.Empty;
        Assert.IsFalse(string.IsNullOrWhiteSpace(workspaceId));
        Assert.AreEqual("sr5", (importResponse["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());

        JsonObject summary = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/summary");
        Assert.AreEqual("Troy Simmons", summary["name"]?.GetValue<string>());
        Assert.AreEqual("BLUE", summary["alias"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Workspace_endpoints_preserve_ruleset_id_from_import_request()
    {
        using var client = CreateClient();

        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        JsonObject importBody = new()
        {
            ["xml"] = xml,
            ["rulesetId"] = "SR6"
        };

        JsonObject importResponse = await PostRequiredJsonObject(client, "/api/workspaces/import", importBody);
        string workspaceId = importResponse["id"]?.GetValue<string>() ?? string.Empty;
        Assert.IsFalse(string.IsNullOrWhiteSpace(workspaceId));
        Assert.AreEqual("sr6", (importResponse["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());

        JsonObject listed = await GetRequiredJsonObject(client, "/api/workspaces");
        JsonArray listedWorkspaces = listed["workspaces"]?.AsArray() ?? [];
        JsonObject listedItem = listedWorkspaces
            .Select(node => node as JsonObject)
            .FirstOrDefault(node => string.Equals(node?["id"]?.GetValue<string>(), workspaceId, StringComparison.Ordinal))
            ?? new JsonObject();
        Assert.IsGreaterThan(0, listedItem.Count, "Expected workspace list entry for imported workspace.");
        Assert.AreEqual("sr6", (listedItem["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());

        JsonObject saveResponse = await PostRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/save", new JsonObject());
        Assert.AreEqual("sr6", (saveResponse["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());

        JsonObject downloadResponse = await PostRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/download", new JsonObject());
        Assert.AreEqual("sr6", (downloadResponse["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());
    }

    [TestMethod]
    public async Task Workspace_list_and_close_endpoints_manage_open_workspace_collection()
    {
        using var client = CreateClient();

        string xmlA = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        string xmlB = File.ReadAllText(FindTestFilePath("BLUE.chum5"));
        JsonObject importBodyA = new() { ["xml"] = xmlA, ["rulesetId"] = "sr5" };
        JsonObject importBodyB = new() { ["xml"] = xmlB, ["rulesetId"] = "sr5" };

        JsonObject importA = await PostRequiredJsonObject(client, "/api/workspaces/import", importBodyA);
        JsonObject importB = await PostRequiredJsonObject(client, "/api/workspaces/import", importBodyB);
        string workspaceA = importA["id"]?.GetValue<string>() ?? string.Empty;
        string workspaceB = importB["id"]?.GetValue<string>() ?? string.Empty;
        Assert.IsFalse(string.IsNullOrWhiteSpace(workspaceA));
        Assert.IsFalse(string.IsNullOrWhiteSpace(workspaceB));

        JsonObject listed = await GetRequiredJsonObject(client, "/api/workspaces");
        Assert.IsGreaterThanOrEqualTo(2, listed["count"]?.GetValue<int>() ?? 0);
        JsonArray listedWorkspaces = listed["workspaces"]?.AsArray() ?? [];
        CollectionAssert.IsSubsetOf(
            new[] { workspaceA, workspaceB },
            listedWorkspaces
                .Select(node => node?["id"]?.GetValue<string>() ?? string.Empty)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToArray());

        using HttpResponseMessage closeResponse = await client.DeleteAsync($"/api/workspaces/{workspaceA}");
        Assert.AreEqual(204, (int)closeResponse.StatusCode);

        JsonObject listedAfterClose = await GetRequiredJsonObject(client, "/api/workspaces");
        JsonArray listedAfterCloseItems = listedAfterClose["workspaces"]?.AsArray() ?? [];
        Assert.IsFalse(listedAfterCloseItems.Any(node => string.Equals(node?["id"]?.GetValue<string>(), workspaceA, StringComparison.Ordinal)));
        Assert.IsTrue(listedAfterCloseItems.Any(node => string.Equals(node?["id"]?.GetValue<string>(), workspaceB, StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Workspace_list_endpoint_honors_maxCount_query_parameter()
    {
        using var client = CreateClient();

        string xmlA = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        string xmlB = File.ReadAllText(FindTestFilePath("BLUE.chum5"));
        await PostRequiredJsonObject(client, "/api/workspaces/import", new JsonObject { ["xml"] = xmlA, ["rulesetId"] = "sr5" });
        await PostRequiredJsonObject(client, "/api/workspaces/import", new JsonObject { ["xml"] = xmlB, ["rulesetId"] = "sr5" });

        JsonObject listed = await GetRequiredJsonObject(client, "/api/workspaces?maxCount=1");
        Assert.AreEqual(1, listed["count"]?.GetValue<int>());
        JsonArray listedWorkspaces = listed["workspaces"]?.AsArray() ?? [];
        Assert.HasCount(1, listedWorkspaces);
    }

    [TestMethod]
    public async Task Workspace_import_returns_bad_request_for_invalid_summary_payload()
    {
        using var client = CreateClient();

        JsonObject payload = new()
        {
            ["xml"] = "<character><name>Broken</name><alias>X</alias><metatype>Human</metatype><buildmethod>Priority</buildmethod><createdversion>1.0</createdversion><appversion>1.0</appversion><karma>not-a-number</karma><nuyen>2500</nuyen><created>True</created></character>",
            ["rulesetId"] = "sr5"
        };

        using StringContent request = new(payload.ToJsonString(), Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync("/api/workspaces/import", request);
        string body = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(400, (int)response.StatusCode, body);
        StringAssert.Contains(body, "error");
    }

    [TestMethod]
    public async Task Workspace_section_endpoint_matches_legacy_section_payload_for_all_sections()
    {
        using var client = CreateClient();

        string xml = File.ReadAllText(FindTestFilePath("BLUE.chum5"));
        JsonObject payload = new()
        {
            ["xml"] = xml,
            ["rulesetId"] = "sr5"
        };

        JsonObject importResponse = await PostRequiredJsonObject(client, "/api/workspaces/import", payload);
        string workspaceId = importResponse["id"]?.GetValue<string>() ?? string.Empty;
        Assert.IsFalse(string.IsNullOrWhiteSpace(workspaceId));

        foreach (string sectionId in AllSectionIds)
        {
            JsonObject legacySection = await PostRequiredJsonObject(client, $"/api/characters/sections/{sectionId}", payload);
            JsonObject workspaceSection = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/sections/{sectionId}");

            Assert.AreEqual(legacySection.ToJsonString(), workspaceSection.ToJsonString(), $"Section mismatch for '{sectionId}'.");
        }
    }

    private static string FindTestFilePath(string fileName)
    {
        string? root = Environment.GetEnvironmentVariable("CHUMMER_REPO_ROOT");
        string[] candidates =
        {
            Path.Combine(Directory.GetCurrentDirectory(), "Chummer.Tests", "TestFiles", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "TestFiles", fileName),
            Path.Combine(AppContext.BaseDirectory, "TestFiles", fileName),
            Path.Combine("/src", "Chummer.Tests", "TestFiles", fileName),
            string.IsNullOrWhiteSpace(root) ? string.Empty : Path.Combine(root, "Chummer.Tests", "TestFiles", fileName)
        };

        string? match = candidates.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));
        if (match is null)
            throw new FileNotFoundException("Could not locate test file.", fileName);

        return match;
    }

    private static HttpClient CreateClient(bool includeApiKey = true)
    {
        var client = new HttpClient
        {
            BaseAddress = BaseUri,
            Timeout = HttpTimeout
        };

        if (includeApiKey && !string.IsNullOrWhiteSpace(ApiKey))
        {
            client.DefaultRequestHeaders.Remove("X-Api-Key");
            client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        }

        return client;
    }

    private static async Task<JsonObject> GetRequiredJsonObject(HttpClient client, string relativePath)
    {
        using HttpResponseMessage response = await client.GetAsync(relativePath);
        string content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(response.IsSuccessStatusCode, $"GET {relativePath} failed with {(int)response.StatusCode}: {content}");

        return ParseRequiredJsonObject(content);
    }

    private static async Task<JsonObject> PostRequiredJsonObject(HttpClient client, string relativePath, JsonObject payload)
    {
        using var request = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync(relativePath, request);
        string content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(response.IsSuccessStatusCode, $"POST {relativePath} failed with {(int)response.StatusCode}: {content}");

        return ParseRequiredJsonObject(content);
    }

    private static async Task<JsonObject> PatchRequiredJsonObject(HttpClient client, string relativePath, JsonObject payload)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, relativePath)
        {
            Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json")
        };
        using HttpResponseMessage response = await client.SendAsync(request);
        string content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(response.IsSuccessStatusCode, $"PATCH {relativePath} failed with {(int)response.StatusCode}: {content}");

        return ParseRequiredJsonObject(content);
    }

    private static async Task<JsonObject> PutRequiredJsonObject(HttpClient client, string relativePath, JsonObject payload)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, relativePath)
        {
            Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json")
        };
        using HttpResponseMessage response = await client.SendAsync(request);
        string content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(response.IsSuccessStatusCode, $"PUT {relativePath} failed with {(int)response.StatusCode}: {content}");

        return ParseRequiredJsonObject(content);
    }

    private static async Task<JsonObject> ParseRequiredJsonObject(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync();
        return ParseRequiredJsonObject(content);
    }

    private static JsonObject ParseRequiredJsonObject(string content)
    {
        JsonNode parsed = JsonNode.Parse(content);
        Assert.IsInstanceOfType<JsonObject>(parsed);
        return (JsonObject)parsed!;
    }

    private static async Task ClearAllWorkspacesAsync(HttpClient client)
    {
        const int maxAttempts = 20;
        const int batchSize = 500;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            JsonObject listed = await GetRequiredJsonObject(client, $"/api/workspaces?maxCount={batchSize}");
            JsonArray workspaces = listed["workspaces"] as JsonArray ?? [];
            if (workspaces.Count == 0)
            {
                return;
            }

            int deletedCount = 0;
            foreach (JsonNode? node in workspaces)
            {
                string workspaceId = node?["id"]?.GetValue<string>() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(workspaceId))
                {
                    continue;
                }

                using HttpResponseMessage response = await client.DeleteAsync($"/api/workspaces/{workspaceId}");
                Assert.IsTrue(
                    response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound,
                    $"DELETE /api/workspaces/{workspaceId} failed with {(int)response.StatusCode}");
                deletedCount++;
            }

            if (deletedCount == 0)
            {
                break;
            }
        }

        JsonObject remaining = await GetRequiredJsonObject(client, "/api/workspaces?maxCount=1");
        JsonArray remainingWorkspaces = remaining["workspaces"] as JsonArray ?? [];
        Assert.IsEmpty(remainingWorkspaces, "Unable to clear all persisted workspaces before running test.");
    }

    private static Uri ResolveBaseUri()
    {
        string? raw = Environment.GetEnvironmentVariable("CHUMMER_API_BASE_URL");
        if (string.IsNullOrWhiteSpace(raw))
            raw = Environment.GetEnvironmentVariable("CHUMMER_WEB_BASE_URL");
        if (string.IsNullOrWhiteSpace(raw))
            raw = "http://chummer-api:8080";

        if (!Uri.TryCreate(raw, UriKind.Absolute, out Uri? uri))
            throw new InvalidOperationException($"Invalid CHUMMER_API_BASE_URL/CHUMMER_WEB_BASE_URL: '{raw}'");

        return uri;
    }

    private static string? ResolveApiKey()
    {
        return Environment.GetEnvironmentVariable("CHUMMER_API_KEY");
    }

    private static string? ResolveExpectedAmendId()
    {
        string? configured = Environment.GetEnvironmentVariable("CHUMMER_AMENDS_EXPECTED_ID");
        if (string.IsNullOrWhiteSpace(configured))
            return null;

        return configured;
    }

    private static TimeSpan ResolveHttpTimeout()
    {
        string? raw = Environment.GetEnvironmentVariable("CHUMMER_API_TEST_TIMEOUT_SECONDS");
        if (int.TryParse(raw, out int seconds) && seconds > 0)
            return TimeSpan.FromSeconds(seconds);

        return TimeSpan.FromSeconds(45);
    }
}
