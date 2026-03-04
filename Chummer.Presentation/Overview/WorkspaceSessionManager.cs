using Chummer.Contracts.Characters;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Overview;

public sealed class WorkspaceSessionManager : IWorkspaceSessionManager
{
    public IReadOnlyList<OpenWorkspaceState> Restore(IReadOnlyList<WorkspaceListItem> workspaces)
    {
        return workspaces
            .Select(workspace => new OpenWorkspaceState(
                Id: workspace.Id,
                Name: string.IsNullOrWhiteSpace(workspace.Summary.Name) ? "(Unnamed Character)" : workspace.Summary.Name,
                Alias: workspace.Summary.Alias ?? string.Empty,
                LastOpenedUtc: workspace.LastUpdatedUtc))
            .OrderByDescending(workspace => workspace.LastOpenedUtc)
            .ToArray();
    }

    public IReadOnlyList<OpenWorkspaceState> Activate(
        IReadOnlyList<OpenWorkspaceState> existing,
        CharacterWorkspaceId id,
        CharacterProfileSection? profile)
    {
        string workspaceName = string.IsNullOrWhiteSpace(profile?.Name) ? "(Unnamed Character)" : profile.Name;
        string workspaceAlias = profile?.Alias ?? string.Empty;
        DateTimeOffset now = DateTimeOffset.UtcNow;

        OpenWorkspaceState[] retained = existing
            .Where(workspace => !string.Equals(workspace.Id.Value, id.Value, StringComparison.Ordinal))
            .ToArray();

        return retained
            .Append(new OpenWorkspaceState(
                Id: id,
                Name: workspaceName,
                Alias: workspaceAlias,
                LastOpenedUtc: now))
            .OrderByDescending(workspace => workspace.LastOpenedUtc)
            .ToArray();
    }

    public IReadOnlyList<OpenWorkspaceState> Close(
        IReadOnlyList<OpenWorkspaceState> existing,
        CharacterWorkspaceId id)
    {
        return existing
            .Where(workspace => !string.Equals(workspace.Id.Value, id.Value, StringComparison.Ordinal))
            .OrderByDescending(workspace => workspace.LastOpenedUtc)
            .ToArray();
    }

    public CharacterWorkspaceId? SelectNext(IReadOnlyList<OpenWorkspaceState> workspaces)
    {
        return workspaces.Count == 0 ? null : workspaces[0].Id;
    }
}
