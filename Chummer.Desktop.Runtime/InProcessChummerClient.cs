using System.Text.Json;
using System.Text.Json.Nodes;
using Chummer.Application.Tools;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Api;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation;

namespace Chummer.Desktop.Runtime;

public sealed class InProcessChummerClient : IChummerClient
{
    private static readonly JsonSerializerOptions SectionJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IWorkspaceService _workspaceService;
    private readonly IRulesetShellCatalogResolver _shellCatalogResolver;
    private readonly IShellPreferencesService _shellPreferencesService;
    private readonly IShellSessionService _shellSessionService;

    public InProcessChummerClient(
        IWorkspaceService workspaceService,
        IRulesetShellCatalogResolver shellCatalogResolver,
        IShellPreferencesService? shellPreferencesService = null,
        IShellSessionService? shellSessionService = null)
    {
        _workspaceService = workspaceService;
        _shellCatalogResolver = shellCatalogResolver;
        _shellPreferencesService = shellPreferencesService ?? new ShellPreferencesService(new InMemoryShellPreferencesStore());
        _shellSessionService = shellSessionService ?? new ShellSessionService(new InMemoryShellSessionStore());
    }

    public Task<WorkspaceImportResult> ImportAsync(WorkspaceImportDocument document, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_workspaceService.Import(document));
    }

    public Task<IReadOnlyList<WorkspaceListItem>> ListWorkspacesAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_workspaceService.List());
    }

    public Task<bool> CloseWorkspaceAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_workspaceService.Close(id));
    }

    public Task<IReadOnlyList<AppCommandDefinition>> GetCommandsAsync(string? rulesetId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_shellCatalogResolver.ResolveCommands(rulesetId));
    }

    public Task<IReadOnlyList<NavigationTabDefinition>> GetNavigationTabsAsync(string? rulesetId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_shellCatalogResolver.ResolveNavigationTabs(rulesetId));
    }

    public Task<ShellPreferences> GetShellPreferencesAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_shellPreferencesService.Load());
    }

    public Task SaveShellPreferencesAsync(ShellPreferences preferences, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _shellPreferencesService.Save(preferences);
        return Task.CompletedTask;
    }

    public Task<ShellSessionState> GetShellSessionAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_shellSessionService.Load());
    }

    public Task SaveShellSessionAsync(ShellSessionState session, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _shellSessionService.Save(new ShellSessionState(
            ActiveWorkspaceId: NormalizeWorkspaceId(session.ActiveWorkspaceId),
            ActiveTabId: NormalizeTabId(session.ActiveTabId),
            ActiveTabsByWorkspace: NormalizeWorkspaceTabMap(session.ActiveTabsByWorkspace)));
        return Task.CompletedTask;
    }

    public Task<ShellBootstrapSnapshot> GetShellBootstrapAsync(string? rulesetId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<WorkspaceListItem> workspaces = _workspaceService.List(ShellBootstrapDefaults.MaxWorkspaces);
        ShellPreferences preferences = _shellPreferencesService.Load();
        ShellSessionState session = _shellSessionService.Load();
        string preferredRulesetId = RulesetDefaults.Normalize(preferences.PreferredRulesetId);
        CharacterWorkspaceId? activeWorkspaceId = ResolveActiveWorkspaceId(workspaces, session.ActiveWorkspaceId);
        string activeRulesetId = ResolveRulesetForWorkspace(activeWorkspaceId, workspaces, preferredRulesetId);
        string effectiveRulesetId = string.IsNullOrWhiteSpace(rulesetId)
            ? activeRulesetId
            : RulesetDefaults.Normalize(rulesetId);

        return Task.FromResult(new ShellBootstrapSnapshot(
            RulesetId: effectiveRulesetId,
            Commands: _shellCatalogResolver.ResolveCommands(effectiveRulesetId),
            NavigationTabs: _shellCatalogResolver.ResolveNavigationTabs(effectiveRulesetId),
            Workspaces: workspaces,
            PreferredRulesetId: preferredRulesetId,
            ActiveRulesetId: activeRulesetId,
            ActiveWorkspaceId: activeWorkspaceId,
            ActiveTabId: session.ActiveTabId,
            ActiveTabsByWorkspace: NormalizeWorkspaceTabMap(session.ActiveTabsByWorkspace)));
    }

    public Task<JsonNode> GetSectionAsync(CharacterWorkspaceId id, string sectionId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        object section = _workspaceService.GetSection(id, sectionId)
            ?? throw new InvalidOperationException($"Section '{sectionId}' was not found for workspace '{id.Value}'.");

        JsonNode? payload = JsonSerializer.SerializeToNode(section, SectionJsonOptions);
        if (payload is null)
        {
            throw new InvalidOperationException($"Section '{sectionId}' returned an empty payload for workspace '{id.Value}'.");
        }

        return Task.FromResult(payload);
    }

    public Task<CharacterFileSummary> GetSummaryAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        CharacterFileSummary summary = RequireWorkspacePayload(
            id,
            _workspaceService.GetSummary(id),
            "Summary");
        return Task.FromResult(summary);
    }

    public Task<CharacterValidationResult> ValidateAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        CharacterValidationResult validation = RequireWorkspacePayload(
            id,
            _workspaceService.Validate(id),
            "Validation");
        return Task.FromResult(validation);
    }

    public Task<CharacterProfileSection> GetProfileAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        CharacterProfileSection profile = RequireWorkspacePayload(
            id,
            _workspaceService.GetProfile(id),
            "Profile");
        return Task.FromResult(profile);
    }

    public Task<CharacterProgressSection> GetProgressAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        CharacterProgressSection progress = RequireWorkspacePayload(
            id,
            _workspaceService.GetProgress(id),
            "Progress");
        return Task.FromResult(progress);
    }

    public Task<CharacterSkillsSection> GetSkillsAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        CharacterSkillsSection skills = RequireWorkspacePayload(
            id,
            _workspaceService.GetSkills(id),
            "Skills");
        return Task.FromResult(skills);
    }

    public Task<CharacterRulesSection> GetRulesAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        CharacterRulesSection rules = RequireWorkspacePayload(
            id,
            _workspaceService.GetRules(id),
            "Rules");
        return Task.FromResult(rules);
    }

    public Task<CharacterBuildSection> GetBuildAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        CharacterBuildSection build = RequireWorkspacePayload(
            id,
            _workspaceService.GetBuild(id),
            "Build");
        return Task.FromResult(build);
    }

    public Task<CharacterMovementSection> GetMovementAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        CharacterMovementSection movement = RequireWorkspacePayload(
            id,
            _workspaceService.GetMovement(id),
            "Movement");
        return Task.FromResult(movement);
    }

    public Task<CharacterAwakeningSection> GetAwakeningAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        CharacterAwakeningSection awakening = RequireWorkspacePayload(
            id,
            _workspaceService.GetAwakening(id),
            "Awakening");
        return Task.FromResult(awakening);
    }

    public Task<CommandResult<CharacterProfileSection>> UpdateMetadataAsync(
        CharacterWorkspaceId id,
        UpdateWorkspaceMetadata command,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_workspaceService.UpdateMetadata(id, command));
    }

    public Task<CommandResult<WorkspaceSaveReceipt>> SaveAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_workspaceService.Save(id));
    }

    public Task<CommandResult<WorkspaceDownloadReceipt>> DownloadAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_workspaceService.Download(id));
    }

    public Task<DataExportBundle> ExportAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        CommandResult<DataExportBundle> result = _workspaceService.Export(id);
        if (!result.Success || result.Value is null)
        {
            throw new InvalidOperationException(result.Error ?? $"Workspace '{id.Value}' was not found.");
        }

        return Task.FromResult(result.Value);
    }

    private static TPayload RequireWorkspacePayload<TPayload>(
        CharacterWorkspaceId id,
        TPayload? payload,
        string payloadName)
        where TPayload : class
    {
        return payload
            ?? throw new InvalidOperationException($"{payloadName} was not found for workspace '{id.Value}'.");
    }

    private static CharacterWorkspaceId? ResolveActiveWorkspaceId(
        IReadOnlyList<WorkspaceListItem> workspaces,
        string? persistedActiveWorkspaceId)
    {
        if (string.IsNullOrWhiteSpace(persistedActiveWorkspaceId))
            return null;

        WorkspaceListItem? matchingWorkspace = workspaces.FirstOrDefault(workspace =>
            string.Equals(workspace.Id.Value, persistedActiveWorkspaceId, StringComparison.Ordinal));
        return matchingWorkspace?.Id;
    }

    private static string ResolveRulesetForWorkspace(
        CharacterWorkspaceId? activeWorkspaceId,
        IReadOnlyList<WorkspaceListItem> workspaces,
        string preferredRulesetId)
    {
        if (activeWorkspaceId is null)
        {
            return RulesetDefaults.Normalize(preferredRulesetId);
        }

        WorkspaceListItem? matchingWorkspace = workspaces.FirstOrDefault(workspace =>
            string.Equals(workspace.Id.Value, activeWorkspaceId.Value.Value, StringComparison.Ordinal));
        return matchingWorkspace is null
            ? RulesetDefaults.Normalize(preferredRulesetId)
            : RulesetDefaults.Normalize(matchingWorkspace.RulesetId);
    }

    private static string? NormalizeWorkspaceId(string? workspaceId)
    {
        return string.IsNullOrWhiteSpace(workspaceId)
            ? null
            : workspaceId.Trim();
    }

    private static string? NormalizeTabId(string? tabId)
    {
        return string.IsNullOrWhiteSpace(tabId)
            ? null
            : tabId.Trim();
    }

    private static IReadOnlyDictionary<string, string>? NormalizeWorkspaceTabMap(IReadOnlyDictionary<string, string>? rawMap)
    {
        if (rawMap is null || rawMap.Count == 0)
        {
            return null;
        }

        Dictionary<string, string> normalized = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> entry in rawMap)
        {
            string? workspaceId = NormalizeWorkspaceId(entry.Key);
            string? tabId = NormalizeTabId(entry.Value);
            if (workspaceId is null || tabId is null)
            {
                continue;
            }

            normalized[workspaceId] = tabId;
        }

        return normalized.Count == 0
            ? null
            : normalized;
    }

    private sealed class InMemoryShellPreferencesStore : IShellPreferencesStore
    {
        private ShellPreferences _preferences = ShellPreferences.Default;

        public ShellPreferences Load()
        {
            return _preferences;
        }

        public void Save(ShellPreferences preferences)
        {
            _preferences = preferences;
        }
    }

    private sealed class InMemoryShellSessionStore : IShellSessionStore
    {
        private ShellSessionState _session = ShellSessionState.Default;

        public ShellSessionState Load()
        {
            return _session;
        }

        public void Save(ShellSessionState session)
        {
            _session = new ShellSessionState(
                ActiveWorkspaceId: NormalizeWorkspaceId(session.ActiveWorkspaceId),
                ActiveTabId: NormalizeTabId(session.ActiveTabId),
                ActiveTabsByWorkspace: NormalizeWorkspaceTabMap(session.ActiveTabsByWorkspace));
        }
    }
}
