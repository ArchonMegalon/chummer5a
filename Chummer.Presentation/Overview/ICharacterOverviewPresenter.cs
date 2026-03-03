using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Overview;

public interface ICharacterOverviewPresenter
{
    CharacterOverviewState State { get; }

    event EventHandler? StateChanged;

    Task InitializeAsync(CancellationToken ct);

    Task ImportAsync(WorkspaceImportDocument document, CancellationToken ct);

    Task LoadAsync(CharacterWorkspaceId id, CancellationToken ct);

    Task UpdateMetadataAsync(UpdateWorkspaceMetadata command, CancellationToken ct);

    Task SaveAsync(CancellationToken ct);
}
