using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Overview;

public interface ICharacterOverviewPresenter
{
    CharacterOverviewState State { get; }

    event EventHandler? StateChanged;

    Task LoadAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task UpdateMetadataAsync(UpdateWorkspaceMetadata command, CancellationToken ct);
}
