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
using Chummer.Application.Owners;
using Chummer.Application.Tools;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Api;
using Chummer.Contracts.Characters;
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
        builder.Services.AddSingleton<ISettingsStore, InMemorySettingsStore>();
        builder.Services.AddSingleton<IRosterStore, InMemoryRosterStore>();
        builder.Services.AddSingleton<IWorkspaceService, InMemoryWorkspaceService>();

        WebApplication app = builder.Build();
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
}
