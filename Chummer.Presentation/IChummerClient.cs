using Chummer.Contracts.Characters;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;
using System.Linq;
using System.Text.Json.Nodes;

namespace Chummer.Presentation;

public interface IChummerClient
{
    Task<ShellUserPreferences> GetShellPreferencesAsync(CancellationToken ct)
    {
        return Task.FromResult(ShellUserPreferences.Default);
    }

    Task SaveShellPreferencesAsync(ShellUserPreferences preferences, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    Task<WorkspaceImportResult> ImportAsync(WorkspaceImportDocument document, CancellationToken ct);

    Task<IReadOnlyList<WorkspaceListItem>> ListWorkspacesAsync(CancellationToken ct);

    Task<bool> CloseWorkspaceAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<IReadOnlyList<AppCommandDefinition>> GetCommandsAsync(string? rulesetId, CancellationToken ct);

    Task<IReadOnlyList<NavigationTabDefinition>> GetNavigationTabsAsync(string? rulesetId, CancellationToken ct);

    async Task<ShellBootstrapSnapshot> GetShellBootstrapAsync(string? rulesetId, CancellationToken ct)
    {
        string normalizedRulesetId = RulesetDefaults.Normalize(rulesetId);
        ShellUserPreferences preferences = await GetShellPreferencesAsync(ct);
        string preferredRulesetId = RulesetDefaults.Normalize(preferences.PreferredRulesetId);
        Task<IReadOnlyList<AppCommandDefinition>> commandsTask = GetCommandsAsync(normalizedRulesetId, ct);
        Task<IReadOnlyList<NavigationTabDefinition>> tabsTask = GetNavigationTabsAsync(normalizedRulesetId, ct);
        Task<IReadOnlyList<WorkspaceListItem>> workspacesTask = ListWorkspacesAsync(ct);
        await Task.WhenAll(commandsTask, tabsTask, workspacesTask);
        string activeRulesetId = RulesetDefaults.Normalize(workspacesTask.Result.FirstOrDefault()?.RulesetId ?? preferredRulesetId);

        return new ShellBootstrapSnapshot(
            RulesetId: normalizedRulesetId,
            Commands: commandsTask.Result,
            NavigationTabs: tabsTask.Result,
            Workspaces: workspacesTask.Result,
            PreferredRulesetId: preferredRulesetId,
            ActiveRulesetId: activeRulesetId);
    }

    Task<JsonNode> GetSectionAsync(CharacterWorkspaceId id, string sectionId, CancellationToken ct);

    Task<CharacterFileSummary> GetSummaryAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CharacterValidationResult> ValidateAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CharacterProfileSection> GetProfileAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CharacterProgressSection> GetProgressAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CharacterSkillsSection> GetSkillsAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CharacterRulesSection> GetRulesAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CharacterBuildSection> GetBuildAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CharacterMovementSection> GetMovementAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CharacterAwakeningSection> GetAwakeningAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CommandResult<CharacterProfileSection>> UpdateMetadataAsync(
        CharacterWorkspaceId id,
        UpdateWorkspaceMetadata command,
        CancellationToken ct);

    Task<CommandResult<WorkspaceSaveReceipt>> SaveAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task<CommandResult<WorkspaceDownloadReceipt>> DownloadAsync(CharacterWorkspaceId id, CancellationToken ct);
}
