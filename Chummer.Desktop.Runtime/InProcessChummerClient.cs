using System.Text.Json;
using System.Text.Json.Nodes;
using Chummer.Application.Workspaces;
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

    public InProcessChummerClient(
        IWorkspaceService workspaceService,
        IRulesetShellCatalogResolver shellCatalogResolver)
    {
        _workspaceService = workspaceService;
        _shellCatalogResolver = shellCatalogResolver;
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

    public Task<ShellBootstrapSnapshot> GetShellBootstrapAsync(string? rulesetId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<WorkspaceListItem> workspaces = _workspaceService.List(ShellBootstrapDefaults.MaxWorkspaces);
        string effectiveRulesetId = string.IsNullOrWhiteSpace(rulesetId)
            ? RulesetDefaults.Normalize(workspaces.FirstOrDefault()?.RulesetId)
            : RulesetDefaults.Normalize(rulesetId);

        return Task.FromResult(new ShellBootstrapSnapshot(
            RulesetId: effectiveRulesetId,
            Commands: _shellCatalogResolver.ResolveCommands(effectiveRulesetId),
            NavigationTabs: _shellCatalogResolver.ResolveNavigationTabs(effectiveRulesetId),
            Workspaces: workspaces));
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

    private static TPayload RequireWorkspacePayload<TPayload>(
        CharacterWorkspaceId id,
        TPayload? payload,
        string payloadName)
        where TPayload : class
    {
        return payload
            ?? throw new InvalidOperationException($"{payloadName} was not found for workspace '{id.Value}'.");
    }
}
