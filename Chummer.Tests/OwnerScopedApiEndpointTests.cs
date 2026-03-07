#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Chummer.Api.Endpoints;
using Chummer.Api.Owners;
using Chummer.Application.Hub;
using Chummer.Application.Owners;
using Chummer.Application.Tools;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Api;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Hub;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Workspaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public sealed class OwnerScopedApiEndpointTests
{
    private const string OwnerHeaderName = "X-Chummer-Owner";

    [TestMethod]
    public async Task Settings_endpoints_isolate_owner_scoped_payloads_when_forwarded_owner_header_is_enabled()
    {
        await using WebApplication app = await CreateAppAsync();
        using HttpClient client = app.GetTestClient();

        await PostRequiredJsonObject(client, "/api/tools/settings/global", new JsonObject
        {
            ["theme"] = "alpha"
        }, "alice@example.com");
        await PostRequiredJsonObject(client, "/api/tools/settings/global", new JsonObject
        {
            ["theme"] = "beta"
        }, "bob@example.com");

        JsonObject alice = await GetRequiredJsonObject(client, "/api/tools/settings/global", "alice@example.com");
        JsonObject bob = await GetRequiredJsonObject(client, "/api/tools/settings/global", "bob@example.com");
        JsonObject local = await GetRequiredJsonObject(client, "/api/tools/settings/global");

        Assert.AreEqual("alpha", alice["settings"]?["theme"]?.GetValue<string>());
        Assert.AreEqual("beta", bob["settings"]?["theme"]?.GetValue<string>());
        Assert.IsNotNull(local["settings"]);
        Assert.IsNull(local["settings"]?["theme"]);
    }

    [TestMethod]
    public async Task Roster_endpoints_isolate_entries_by_forwarded_owner_header()
    {
        await using WebApplication app = await CreateAppAsync();
        using HttpClient client = app.GetTestClient();

        await PostRequiredJsonObject(client, "/api/tools/roster", new JsonObject
        {
            ["name"] = "Alpha",
            ["alias"] = "A"
        }, "alice@example.com");
        await PostRequiredJsonObject(client, "/api/tools/roster", new JsonObject
        {
            ["name"] = "Beta",
            ["alias"] = "B"
        }, "bob@example.com");

        JsonObject alice = await GetRequiredJsonObject(client, "/api/tools/roster", "alice@example.com");
        JsonObject bob = await GetRequiredJsonObject(client, "/api/tools/roster", "bob@example.com");
        JsonObject local = await GetRequiredJsonObject(client, "/api/tools/roster");

        Assert.AreEqual(1, alice["count"]?.GetValue<int>());
        Assert.AreEqual("Alpha", alice["entries"]?[0]?["name"]?.GetValue<string>());
        Assert.AreEqual(1, bob["count"]?.GetValue<int>());
        Assert.AreEqual("Beta", bob["entries"]?[0]?["name"]?.GetValue<string>());
        Assert.AreEqual(0, local["count"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Workspace_endpoints_isolate_imported_workspaces_by_forwarded_owner_header()
    {
        await using WebApplication app = await CreateAppAsync();
        using HttpClient client = app.GetTestClient();

        JsonObject import = await PostRequiredJsonObject(client, "/api/workspaces/import", new JsonObject
        {
            ["xml"] = "<character><name>Owner Runner</name></character>",
            ["rulesetId"] = "sr5"
        }, "alice@example.com");
        string workspaceId = import["id"]?.GetValue<string>() ?? string.Empty;

        JsonObject alice = await GetRequiredJsonObject(client, "/api/workspaces", "alice@example.com");
        JsonObject bob = await GetRequiredJsonObject(client, "/api/workspaces", "bob@example.com");
        JsonObject local = await GetRequiredJsonObject(client, "/api/workspaces");

        Assert.AreEqual(1, alice["count"]?.GetValue<int>());
        Assert.AreEqual(workspaceId, alice["workspaces"]?[0]?["id"]?.GetValue<string>());
        Assert.AreEqual(0, bob["count"]?.GetValue<int>());
        Assert.AreEqual(0, local["count"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Hub_publication_endpoints_isolate_drafts_by_forwarded_owner_header()
    {
        await using WebApplication app = await CreateAppAsync();
        using HttpClient client = app.GetTestClient();

        await PostRequiredJsonObject(client, "/api/hub/publish/drafts", new JsonObject
        {
            ["projectKind"] = HubCatalogItemKinds.RulePack,
            ["projectId"] = "alice.pack",
            ["rulesetId"] = "sr5",
            ["title"] = "Alice Pack"
        }, "alice@example.com");
        await PostRequiredJsonObject(client, "/api/hub/publish/drafts", new JsonObject
        {
            ["projectKind"] = HubCatalogItemKinds.RulePack,
            ["projectId"] = "bob.pack",
            ["rulesetId"] = "sr5",
            ["title"] = "Bob Pack"
        }, "bob@example.com");

        JsonObject alice = await GetRequiredJsonObject(client, "/api/hub/publish/drafts?ruleset=sr5", "alice@example.com");
        JsonObject bob = await GetRequiredJsonObject(client, "/api/hub/publish/drafts?ruleset=sr5", "bob@example.com");
        JsonObject local = await GetRequiredJsonObject(client, "/api/hub/publish/drafts?ruleset=sr5");

        Assert.AreEqual(1, alice["items"]?.AsArray().Count);
        Assert.AreEqual("alice.pack", alice["items"]?[0]?["projectId"]?.GetValue<string>());
        Assert.AreEqual(1, bob["items"]?.AsArray().Count);
        Assert.AreEqual("bob.pack", bob["items"]?[0]?["projectId"]?.GetValue<string>());
        Assert.AreEqual(0, local["items"]?.AsArray().Count);
    }

    [TestMethod]
    public async Task Hub_publication_detail_endpoint_respects_forwarded_owner_scope()
    {
        await using WebApplication app = await CreateAppAsync();
        using HttpClient client = app.GetTestClient();

        JsonObject aliceDraft = await PostRequiredJsonObject(client, "/api/hub/publish/drafts", new JsonObject
        {
            ["projectKind"] = HubCatalogItemKinds.RulePack,
            ["projectId"] = "alice.pack.detail",
            ["rulesetId"] = "sr5",
            ["title"] = "Alice Pack Detail"
        }, "alice@example.com");
        string draftId = aliceDraft["draftId"]?.GetValue<string>() ?? string.Empty;

        JsonObject alice = await GetRequiredJsonObject(client, $"/api/hub/publish/drafts/{draftId}", "alice@example.com");
        Assert.AreEqual(draftId, alice["draft"]?["draftId"]?.GetValue<string>());

        using HttpRequestMessage request = new(HttpMethod.Get, $"/api/hub/publish/drafts/{draftId}");
        request.Headers.Add(OwnerHeaderName, "bob@example.com");
        using HttpResponseMessage response = await client.SendAsync(request);
        Assert.AreEqual(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [TestMethod]
    public async Task Hub_publication_update_endpoint_respects_forwarded_owner_scope()
    {
        await using WebApplication app = await CreateAppAsync();
        using HttpClient client = app.GetTestClient();

        JsonObject aliceDraft = await PostRequiredJsonObject(client, "/api/hub/publish/drafts", new JsonObject
        {
            ["projectKind"] = HubCatalogItemKinds.RulePack,
            ["projectId"] = "alice.pack.update",
            ["rulesetId"] = "sr5",
            ["title"] = "Alice Pack Update"
        }, "alice@example.com");
        string draftId = aliceDraft["draftId"]?.GetValue<string>() ?? string.Empty;

        JsonObject updated = await PutRequiredJsonObject(client, $"/api/hub/publish/drafts/{draftId}", new JsonObject
        {
            ["title"] = "Alice Pack Updated",
            ["summary"] = "Street-level runtime",
            ["description"] = "Campaign-specific SR5 publication draft."
        }, "alice@example.com");
        Assert.AreEqual("Alice Pack Updated", updated["title"]?.GetValue<string>());
        Assert.AreEqual("Street-level runtime", updated["summary"]?.GetValue<string>());

        using HttpRequestMessage request = new(HttpMethod.Put, $"/api/hub/publish/drafts/{draftId}")
        {
            Content = JsonContent.Create(new JsonObject
            {
                ["title"] = "Bob Cannot Update This",
                ["summary"] = "blocked"
            })
        };
        request.Headers.Add(OwnerHeaderName, "bob@example.com");
        using HttpResponseMessage response = await client.SendAsync(request);
        Assert.AreEqual(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [TestMethod]
    public async Task Hub_publication_archive_and_delete_endpoints_respect_forwarded_owner_scope()
    {
        await using WebApplication app = await CreateAppAsync();
        using HttpClient client = app.GetTestClient();

        JsonObject aliceDraft = await PostRequiredJsonObject(client, "/api/hub/publish/drafts", new JsonObject
        {
            ["projectKind"] = HubCatalogItemKinds.RulePack,
            ["projectId"] = "alice.pack.lifecycle",
            ["rulesetId"] = "sr5",
            ["title"] = "Alice Pack Lifecycle"
        }, "alice@example.com");
        string draftId = aliceDraft["draftId"]?.GetValue<string>() ?? string.Empty;

        JsonObject archived = await PostRequiredJsonObject(client, $"/api/hub/publish/drafts/{draftId}/archive", new JsonObject(), "alice@example.com");
        Assert.AreEqual(HubPublicationStates.Archived, archived["state"]?.GetValue<string>());

        using HttpRequestMessage archiveRequest = new(HttpMethod.Post, $"/api/hub/publish/drafts/{draftId}/archive")
        {
            Content = JsonContent.Create(new JsonObject())
        };
        archiveRequest.Headers.Add(OwnerHeaderName, "bob@example.com");
        using HttpResponseMessage archiveResponse = await client.SendAsync(archiveRequest);
        Assert.AreEqual(StatusCodes.Status404NotFound, (int)archiveResponse.StatusCode);

        using HttpRequestMessage deleteRequest = new(HttpMethod.Delete, $"/api/hub/publish/drafts/{draftId}");
        deleteRequest.Headers.Add(OwnerHeaderName, "alice@example.com");
        using HttpResponseMessage deleteResponse = await client.SendAsync(deleteRequest);
        Assert.AreEqual(StatusCodes.Status204NoContent, (int)deleteResponse.StatusCode);

        using HttpRequestMessage bobDeleteRequest = new(HttpMethod.Delete, $"/api/hub/publish/drafts/{draftId}");
        bobDeleteRequest.Headers.Add(OwnerHeaderName, "bob@example.com");
        using HttpResponseMessage bobDeleteResponse = await client.SendAsync(bobDeleteRequest);
        Assert.AreEqual(StatusCodes.Status404NotFound, (int)bobDeleteResponse.StatusCode);
    }

    [TestMethod]
    public async Task Hub_moderation_action_endpoints_respect_forwarded_owner_scope()
    {
        await using WebApplication app = await CreateAppAsync();
        using HttpClient client = app.GetTestClient();

        await PostRequiredJsonObject(client, "/api/hub/publish/drafts", new JsonObject
        {
            ["projectKind"] = HubCatalogItemKinds.RulePack,
            ["projectId"] = "alice.pack.moderation",
            ["rulesetId"] = "sr5",
            ["title"] = "Alice Pack Moderation"
        }, "alice@example.com");
        JsonObject submission = await PostRequiredJsonObject(client, "/api/hub/publish/rulepack/alice.pack.moderation/submit?ruleset=sr5", new JsonObject
        {
            ["notes"] = "ready"
        }, "alice@example.com");
        string caseId = submission["caseId"]?.GetValue<string>() ?? string.Empty;

        JsonObject approved = await PostRequiredJsonObject(client, $"/api/hub/moderation/queue/{caseId}/approve", new JsonObject
        {
            ["notes"] = "approved"
        }, "alice@example.com");
        Assert.AreEqual(HubModerationStates.Approved, approved["state"]?.GetValue<string>());

        using HttpRequestMessage rejectRequest = new(HttpMethod.Post, $"/api/hub/moderation/queue/{caseId}/reject")
        {
            Content = JsonContent.Create(new JsonObject
            {
                ["notes"] = "bob cannot update this"
            })
        };
        rejectRequest.Headers.Add(OwnerHeaderName, "bob@example.com");
        using HttpResponseMessage rejectResponse = await client.SendAsync(rejectRequest);
        Assert.AreEqual(StatusCodes.Status404NotFound, (int)rejectResponse.StatusCode);
    }

    [TestMethod]
    public async Task Hub_publisher_endpoints_respect_forwarded_owner_scope()
    {
        await using WebApplication app = await CreateAppAsync();
        using HttpClient client = app.GetTestClient();

        JsonObject alicePublisher = await PutRequiredJsonObject(client, "/api/hub/publishers/shadowops", new JsonObject
        {
            ["displayName"] = "ShadowOps",
            ["slug"] = "shadowops",
            ["description"] = "Campaign runtime publisher"
        }, "alice@example.com");
        Assert.AreEqual("shadowops", alicePublisher["publisherId"]?.GetValue<string>());

        JsonObject aliceList = await GetRequiredJsonObject(client, "/api/hub/publishers", "alice@example.com");
        JsonObject bobList = await GetRequiredJsonObject(client, "/api/hub/publishers", "bob@example.com");

        Assert.AreEqual(1, aliceList["items"]?.AsArray().Count);
        Assert.AreEqual(0, bobList["items"]?.AsArray().Count);

        using HttpRequestMessage request = new(HttpMethod.Get, "/api/hub/publishers/shadowops");
        request.Headers.Add(OwnerHeaderName, "bob@example.com");
        using HttpResponseMessage response = await client.SendAsync(request);
        Assert.AreEqual(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [TestMethod]
    public async Task Hub_review_endpoints_respect_forwarded_owner_scope()
    {
        await using WebApplication app = await CreateAppAsync();
        using HttpClient client = app.GetTestClient();

        JsonObject aliceReview = await PutRequiredJsonObject(client, "/api/hub/reviews/rulepack/alice.pack", new JsonObject
        {
            ["rulesetId"] = "sr5",
            ["recommendationState"] = HubRecommendationStates.Recommended,
            ["stars"] = 5,
            ["reviewText"] = "Great pack",
            ["usedAtTable"] = true
        }, "alice@example.com");
        Assert.AreEqual(HubRecommendationStates.Recommended, aliceReview["recommendationState"]?.GetValue<string>());

        JsonObject aliceList = await GetRequiredJsonObject(client, "/api/hub/reviews?kind=rulepack&itemId=alice.pack&ruleset=sr5", "alice@example.com");
        JsonObject bobList = await GetRequiredJsonObject(client, "/api/hub/reviews?kind=rulepack&itemId=alice.pack&ruleset=sr5", "bob@example.com");

        Assert.AreEqual(1, aliceList["items"]?.AsArray().Count);
        Assert.AreEqual(0, bobList["items"]?.AsArray().Count);
    }

    private static async Task<WebApplication> CreateAppAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<IOwnerContextAccessor>(provider =>
            new RequestOwnerContextAccessor(
                provider.GetRequiredService<IHttpContextAccessor>(),
                OwnerHeaderName));
        builder.Services.AddSingleton<IHubPublisherStore, InMemoryHubPublisherStore>();
        builder.Services.AddSingleton<IHubPublisherService, DefaultHubPublisherService>();
        builder.Services.AddSingleton<IHubReviewStore, InMemoryHubReviewStore>();
        builder.Services.AddSingleton<IHubReviewService, DefaultHubReviewService>();
        builder.Services.AddSingleton<IHubDraftStore, InMemoryHubDraftStore>();
        builder.Services.AddSingleton<IHubModerationCaseStore, InMemoryHubModerationCaseStore>();
        builder.Services.AddSingleton<IHubPublicationService, DefaultHubPublicationService>();
        builder.Services.AddSingleton<IHubModerationService, DefaultHubModerationService>();
        builder.Services.AddSingleton<ISettingsStore, InMemorySettingsStore>();
        builder.Services.AddSingleton<IRosterStore, InMemoryRosterStore>();
        builder.Services.AddSingleton<IWorkspaceService, InMemoryWorkspaceService>();

        WebApplication app = builder.Build();
        app.MapHubPublisherEndpoints();
        app.MapHubReviewEndpoints();
        app.MapHubPublicationEndpoints();
        app.MapSettingsEndpoints();
        app.MapRosterEndpoints();
        app.MapWorkspaceEndpoints();
        await app.StartAsync();
        return app;
    }

    private static async Task<JsonObject> GetRequiredJsonObject(HttpClient client, string relativePath, string? owner = null)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, relativePath);
        if (!string.IsNullOrWhiteSpace(owner))
        {
            request.Headers.Add(OwnerHeaderName, owner);
        }

        using HttpResponseMessage response = await client.SendAsync(request);
        string content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(response.IsSuccessStatusCode, $"GET {relativePath} failed with {(int)response.StatusCode}: {content}");
        JsonNode? parsed = JsonNode.Parse(content);
        Assert.IsInstanceOfType<JsonObject>(parsed);
        return (JsonObject)parsed;
    }

    private static async Task<JsonObject> PostRequiredJsonObject(HttpClient client, string relativePath, JsonObject payload, string? owner = null)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, relativePath)
        {
            Content = JsonContent.Create(payload)
        };
        if (!string.IsNullOrWhiteSpace(owner))
        {
            request.Headers.Add(OwnerHeaderName, owner);
        }

        using HttpResponseMessage response = await client.SendAsync(request);
        string content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(response.IsSuccessStatusCode, $"POST {relativePath} failed with {(int)response.StatusCode}: {content}");
        JsonNode? parsed = JsonNode.Parse(content);
        Assert.IsInstanceOfType<JsonObject>(parsed);
        return (JsonObject)parsed;
    }

    private static async Task<JsonObject> PutRequiredJsonObject(HttpClient client, string relativePath, JsonObject payload, string? owner = null)
    {
        using HttpRequestMessage request = new(HttpMethod.Put, relativePath)
        {
            Content = JsonContent.Create(payload)
        };
        if (!string.IsNullOrWhiteSpace(owner))
        {
            request.Headers.Add(OwnerHeaderName, owner);
        }

        using HttpResponseMessage response = await client.SendAsync(request);
        string content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(response.IsSuccessStatusCode, $"PUT {relativePath} failed with {(int)response.StatusCode}: {content}");
        JsonNode? parsed = JsonNode.Parse(content);
        Assert.IsInstanceOfType<JsonObject>(parsed);
        return (JsonObject)parsed;
    }

    private sealed class InMemorySettingsStore : ISettingsStore
    {
        private readonly Dictionary<(string Owner, string Scope), JsonObject> _store = new();

        public JsonObject Load(string scope) => Load(OwnerScope.LocalSingleUser, scope);

        public JsonObject Load(OwnerScope owner, string scope)
        {
            return _store.TryGetValue((owner.NormalizedValue, scope), out JsonObject? settings)
                ? JsonNode.Parse(settings.ToJsonString())?.AsObject() ?? new JsonObject()
                : new JsonObject();
        }

        public void Save(string scope, JsonObject settings) => Save(OwnerScope.LocalSingleUser, scope, settings);

        public void Save(OwnerScope owner, string scope, JsonObject settings)
        {
            _store[(owner.NormalizedValue, scope)] = JsonNode.Parse(settings.ToJsonString())?.AsObject() ?? new JsonObject();
        }
    }

    private sealed class InMemoryRosterStore : IRosterStore
    {
        private readonly Dictionary<string, List<RosterEntry>> _entriesByOwner = new(StringComparer.Ordinal);

        public IReadOnlyList<RosterEntry> Load() => Load(OwnerScope.LocalSingleUser);

        public IReadOnlyList<RosterEntry> Load(OwnerScope owner)
        {
            return _entriesByOwner.TryGetValue(owner.NormalizedValue, out List<RosterEntry>? entries)
                ? entries.ToArray()
                : Array.Empty<RosterEntry>();
        }

        public IReadOnlyList<RosterEntry> Upsert(RosterEntry entry) => Upsert(OwnerScope.LocalSingleUser, entry);

        public IReadOnlyList<RosterEntry> Upsert(OwnerScope owner, RosterEntry entry)
        {
            if (!_entriesByOwner.TryGetValue(owner.NormalizedValue, out List<RosterEntry>? entries))
            {
                entries = [];
                _entriesByOwner[owner.NormalizedValue] = entries;
            }

            int existingIndex = entries.FindIndex(candidate =>
                string.Equals(candidate.Name, entry.Name, StringComparison.Ordinal)
                && string.Equals(candidate.Alias, entry.Alias, StringComparison.Ordinal));
            if (existingIndex >= 0)
            {
                entries[existingIndex] = entry;
            }
            else
            {
                entries.Add(entry);
            }

            return entries.ToArray();
        }
    }

    private sealed class InMemoryWorkspaceService : IWorkspaceService
    {
        private readonly Dictionary<string, List<WorkspaceListItem>> _workspacesByOwner = new(StringComparer.Ordinal);

        public WorkspaceImportResult Import(WorkspaceImportDocument document) => Import(OwnerScope.LocalSingleUser, document);

        public WorkspaceImportResult Import(OwnerScope owner, WorkspaceImportDocument document)
        {
            string ownerKey = owner.NormalizedValue;
            if (!_workspacesByOwner.TryGetValue(ownerKey, out List<WorkspaceListItem>? workspaces))
            {
                workspaces = [];
                _workspacesByOwner[ownerKey] = workspaces;
            }

            CharacterWorkspaceId id = new(Guid.NewGuid().ToString("N"));
            CharacterFileSummary summary = new(
                Name: "Owner Runner",
                Alias: string.Empty,
                Metatype: "Human",
                BuildMethod: "Priority",
                CreatedVersion: "5",
                AppVersion: "5",
                Karma: 0m,
                Nuyen: 0m,
                Created: true);
            workspaces.Add(new WorkspaceListItem(
                Id: id,
                Summary: summary,
                LastUpdatedUtc: DateTimeOffset.UtcNow,
                RulesetId: document.RulesetId));

            return new WorkspaceImportResult(id, summary, document.RulesetId);
        }

        public IReadOnlyList<WorkspaceListItem> List(int? maxCount = null) => List(OwnerScope.LocalSingleUser, maxCount);

        public IReadOnlyList<WorkspaceListItem> List(OwnerScope owner, int? maxCount = null)
        {
            if (!_workspacesByOwner.TryGetValue(owner.NormalizedValue, out List<WorkspaceListItem>? workspaces))
            {
                return Array.Empty<WorkspaceListItem>();
            }

            return maxCount is > 0
                ? workspaces.Take(maxCount.Value).ToArray()
                : workspaces.ToArray();
        }

        public bool Close(CharacterWorkspaceId id) => Close(OwnerScope.LocalSingleUser, id);

        public bool Close(OwnerScope owner, CharacterWorkspaceId id)
        {
            if (!_workspacesByOwner.TryGetValue(owner.NormalizedValue, out List<WorkspaceListItem>? workspaces))
            {
                return false;
            }

            return workspaces.RemoveAll(workspace => string.Equals(workspace.Id.Value, id.Value, StringComparison.Ordinal)) > 0;
        }

        public object? GetSection(CharacterWorkspaceId id, string sectionId) => throw new NotSupportedException();

        public object? GetSection(OwnerScope owner, CharacterWorkspaceId id, string sectionId) => throw new NotSupportedException();

        public CharacterFileSummary? GetSummary(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterFileSummary? GetSummary(OwnerScope owner, CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterValidationResult? Validate(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterValidationResult? Validate(OwnerScope owner, CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterProfileSection? GetProfile(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterProfileSection? GetProfile(OwnerScope owner, CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterProgressSection? GetProgress(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterProgressSection? GetProgress(OwnerScope owner, CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterSkillsSection? GetSkills(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterSkillsSection? GetSkills(OwnerScope owner, CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterRulesSection? GetRules(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterRulesSection? GetRules(OwnerScope owner, CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterBuildSection? GetBuild(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterBuildSection? GetBuild(OwnerScope owner, CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterMovementSection? GetMovement(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterMovementSection? GetMovement(OwnerScope owner, CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterAwakeningSection? GetAwakening(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CharacterAwakeningSection? GetAwakening(OwnerScope owner, CharacterWorkspaceId id) => throw new NotSupportedException();

        public CommandResult<CharacterProfileSection> UpdateMetadata(CharacterWorkspaceId id, UpdateWorkspaceMetadata command) => throw new NotSupportedException();

        public CommandResult<CharacterProfileSection> UpdateMetadata(OwnerScope owner, CharacterWorkspaceId id, UpdateWorkspaceMetadata command) => throw new NotSupportedException();

        public CommandResult<WorkspaceSaveReceipt> Save(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CommandResult<WorkspaceSaveReceipt> Save(OwnerScope owner, CharacterWorkspaceId id) => throw new NotSupportedException();

        public CommandResult<WorkspaceDownloadReceipt> Download(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CommandResult<WorkspaceDownloadReceipt> Download(OwnerScope owner, CharacterWorkspaceId id) => throw new NotSupportedException();

        public CommandResult<WorkspaceExportReceipt> Export(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CommandResult<WorkspaceExportReceipt> Export(OwnerScope owner, CharacterWorkspaceId id) => throw new NotSupportedException();

        public CommandResult<WorkspacePrintReceipt> Print(CharacterWorkspaceId id) => throw new NotSupportedException();

        public CommandResult<WorkspacePrintReceipt> Print(OwnerScope owner, CharacterWorkspaceId id) => throw new NotSupportedException();
    }

    private sealed class InMemoryHubDraftStore : IHubDraftStore
    {
        private readonly Dictionary<string, List<HubDraftRecord>> _recordsByOwner = new(StringComparer.Ordinal);

        public IReadOnlyList<HubDraftRecord> List(OwnerScope owner, string? kind = null, string? rulesetId = null, string? state = null)
        {
            return _recordsByOwner.TryGetValue(owner.NormalizedValue, out List<HubDraftRecord>? records)
                ? records
                    .Where(record => kind is null || string.Equals(record.ProjectKind, kind, StringComparison.Ordinal))
                    .Where(record => rulesetId is null || string.Equals(record.RulesetId, rulesetId, StringComparison.Ordinal))
                    .Where(record => state is null || string.Equals(record.State, state, StringComparison.Ordinal))
                    .ToArray()
                : Array.Empty<HubDraftRecord>();
        }

        public HubDraftRecord? Get(OwnerScope owner, string kind, string projectId, string rulesetId)
        {
            return List(owner, kind, rulesetId).FirstOrDefault(record => string.Equals(record.ProjectId, projectId, StringComparison.Ordinal));
        }

        public HubDraftRecord? Get(OwnerScope owner, string draftId)
        {
            return List(owner).FirstOrDefault(record => string.Equals(record.DraftId, draftId, StringComparison.Ordinal));
        }

        public HubDraftRecord Upsert(OwnerScope owner, HubDraftRecord record)
        {
            if (!_recordsByOwner.TryGetValue(owner.NormalizedValue, out List<HubDraftRecord>? records))
            {
                records = [];
                _recordsByOwner[owner.NormalizedValue] = records;
            }

            int existingIndex = records.FindIndex(current =>
                string.Equals(current.ProjectKind, record.ProjectKind, StringComparison.Ordinal)
                && string.Equals(current.ProjectId, record.ProjectId, StringComparison.Ordinal)
                && string.Equals(current.RulesetId, record.RulesetId, StringComparison.Ordinal));
            HubDraftRecord normalizedRecord = record with { OwnerId = owner.NormalizedValue };
            if (existingIndex >= 0)
            {
                records[existingIndex] = normalizedRecord;
            }
            else
            {
                records.Add(normalizedRecord);
            }

            return normalizedRecord;
        }

        public bool Delete(OwnerScope owner, string draftId)
        {
            if (!_recordsByOwner.TryGetValue(owner.NormalizedValue, out List<HubDraftRecord>? records))
            {
                return false;
            }

            return records.RemoveAll(record => string.Equals(record.DraftId, draftId, StringComparison.Ordinal)) > 0;
        }
    }

    private sealed class InMemoryHubPublisherStore : IHubPublisherStore
    {
        private readonly Dictionary<string, List<HubPublisherRecord>> _recordsByOwner = new(StringComparer.Ordinal);

        public IReadOnlyList<HubPublisherRecord> List(OwnerScope owner)
        {
            return _recordsByOwner.TryGetValue(owner.NormalizedValue, out List<HubPublisherRecord>? records)
                ? records.ToArray()
                : Array.Empty<HubPublisherRecord>();
        }

        public HubPublisherRecord? Get(OwnerScope owner, string publisherId)
        {
            return List(owner).FirstOrDefault(record => string.Equals(record.PublisherId, publisherId, StringComparison.Ordinal));
        }

        public HubPublisherRecord Upsert(OwnerScope owner, HubPublisherRecord record)
        {
            if (!_recordsByOwner.TryGetValue(owner.NormalizedValue, out List<HubPublisherRecord>? records))
            {
                records = [];
                _recordsByOwner[owner.NormalizedValue] = records;
            }

            int existingIndex = records.FindIndex(current =>
                string.Equals(current.PublisherId, record.PublisherId, StringComparison.Ordinal));
            HubPublisherRecord normalizedRecord = record with { OwnerId = owner.NormalizedValue };
            if (existingIndex >= 0)
            {
                records[existingIndex] = normalizedRecord;
            }
            else
            {
                records.Add(normalizedRecord);
            }

            return normalizedRecord;
        }
    }

    private sealed class InMemoryHubModerationCaseStore : IHubModerationCaseStore
    {
        private readonly Dictionary<string, List<HubModerationCaseRecord>> _recordsByOwner = new(StringComparer.Ordinal);

        public IReadOnlyList<HubModerationCaseRecord> List(OwnerScope owner, string? kind = null, string? rulesetId = null, string? state = null)
        {
            return _recordsByOwner.TryGetValue(owner.NormalizedValue, out List<HubModerationCaseRecord>? records)
                ? records
                    .Where(record => kind is null || string.Equals(record.ProjectKind, kind, StringComparison.Ordinal))
                    .Where(record => rulesetId is null || string.Equals(record.RulesetId, rulesetId, StringComparison.Ordinal))
                    .Where(record => state is null || string.Equals(record.State, state, StringComparison.Ordinal))
                    .ToArray()
                : Array.Empty<HubModerationCaseRecord>();
        }

        public HubModerationCaseRecord? Get(OwnerScope owner, string kind, string projectId, string rulesetId)
        {
            return List(owner, kind, rulesetId).FirstOrDefault(record => string.Equals(record.ProjectId, projectId, StringComparison.Ordinal));
        }

        public HubModerationCaseRecord? GetByCaseId(OwnerScope owner, string caseId)
        {
            return List(owner).FirstOrDefault(record => string.Equals(record.CaseId, caseId, StringComparison.Ordinal));
        }

        public HubModerationCaseRecord? GetByDraftId(OwnerScope owner, string draftId)
        {
            return List(owner).FirstOrDefault(record => string.Equals(record.DraftId, draftId, StringComparison.Ordinal));
        }

        public HubModerationCaseRecord Upsert(OwnerScope owner, HubModerationCaseRecord record)
        {
            if (!_recordsByOwner.TryGetValue(owner.NormalizedValue, out List<HubModerationCaseRecord>? records))
            {
                records = [];
                _recordsByOwner[owner.NormalizedValue] = records;
            }

            int existingIndex = records.FindIndex(current =>
                string.Equals(current.ProjectKind, record.ProjectKind, StringComparison.Ordinal)
                && string.Equals(current.ProjectId, record.ProjectId, StringComparison.Ordinal)
                && string.Equals(current.RulesetId, record.RulesetId, StringComparison.Ordinal));
            HubModerationCaseRecord normalizedRecord = record with { OwnerId = owner.NormalizedValue };
            if (existingIndex >= 0)
            {
                records[existingIndex] = normalizedRecord;
            }
            else
            {
                records.Add(normalizedRecord);
            }

            return normalizedRecord;
        }

        public bool DeleteByDraftId(OwnerScope owner, string draftId)
        {
            if (!_recordsByOwner.TryGetValue(owner.NormalizedValue, out List<HubModerationCaseRecord>? records))
            {
                return false;
            }

            return records.RemoveAll(record => string.Equals(record.DraftId, draftId, StringComparison.Ordinal)) > 0;
        }
    }

    private sealed class InMemoryHubReviewStore : IHubReviewStore
    {
        private readonly Dictionary<string, List<HubReviewRecord>> _recordsByOwner = new(StringComparer.Ordinal);

        public IReadOnlyList<HubReviewRecord> List(OwnerScope owner, string? kind = null, string? itemId = null, string? rulesetId = null)
        {
            return _recordsByOwner.TryGetValue(owner.NormalizedValue, out List<HubReviewRecord>? records)
                ? records
                    .Where(record => kind is null || string.Equals(record.ProjectKind, kind, StringComparison.Ordinal))
                    .Where(record => itemId is null || string.Equals(record.ProjectId, itemId, StringComparison.Ordinal))
                    .Where(record => rulesetId is null || string.Equals(record.RulesetId, rulesetId, StringComparison.Ordinal))
                    .ToArray()
                : Array.Empty<HubReviewRecord>();
        }

        public HubReviewRecord? Get(OwnerScope owner, string kind, string itemId, string rulesetId)
        {
            if (!_recordsByOwner.TryGetValue(owner.NormalizedValue, out List<HubReviewRecord>? records))
            {
                return null;
            }

            return records.Find(record =>
                string.Equals(record.ProjectKind, kind, StringComparison.Ordinal)
                && string.Equals(record.ProjectId, itemId, StringComparison.Ordinal)
                && string.Equals(record.RulesetId, rulesetId, StringComparison.Ordinal));
        }

        public HubReviewRecord Upsert(OwnerScope owner, HubReviewRecord record)
        {
            if (!_recordsByOwner.TryGetValue(owner.NormalizedValue, out List<HubReviewRecord>? records))
            {
                records = [];
                _recordsByOwner[owner.NormalizedValue] = records;
            }

            int existingIndex = records.FindIndex(current =>
                string.Equals(current.ProjectKind, record.ProjectKind, StringComparison.Ordinal)
                && string.Equals(current.ProjectId, record.ProjectId, StringComparison.Ordinal)
                && string.Equals(current.RulesetId, record.RulesetId, StringComparison.Ordinal));
            HubReviewRecord normalizedRecord = record with { OwnerId = owner.NormalizedValue };
            if (existingIndex >= 0)
            {
                records[existingIndex] = normalizedRecord;
            }
            else
            {
                records.Add(normalizedRecord);
            }

            return normalizedRecord;
        }
    }
}
