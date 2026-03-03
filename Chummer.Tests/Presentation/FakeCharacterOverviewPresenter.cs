using System;
using System.Threading;
using System.Threading.Tasks;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Overview;

namespace Chummer.Tests.Presentation;

internal sealed class FakeCharacterOverviewPresenter : ICharacterOverviewPresenter
{
    public CharacterOverviewState State { get; private set; } = CharacterOverviewState.Empty;

    public event EventHandler? StateChanged;

    public CharacterWorkspaceId? LoadedWorkspaceId { get; private set; }

    public string? ImportedContent { get; private set; }

    public UpdateWorkspaceMetadata? UpdatedMetadata { get; private set; }

    public int SaveCalls { get; private set; }

    public int InitializeCalls { get; private set; }

    public Task InitializeAsync(CancellationToken ct)
    {
        InitializeCalls++;
        return Task.CompletedTask;
    }

    public Task ImportAsync(WorkspaceImportDocument document, CancellationToken ct)
    {
        ImportedContent = document.Content;
        return Task.CompletedTask;
    }

    public Task LoadAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        LoadedWorkspaceId = id;
        return Task.CompletedTask;
    }

    public Task UpdateMetadataAsync(UpdateWorkspaceMetadata command, CancellationToken ct)
    {
        UpdatedMetadata = command;
        return Task.CompletedTask;
    }

    public Task SaveAsync(CancellationToken ct)
    {
        SaveCalls++;
        return Task.CompletedTask;
    }

    public void Publish(CharacterOverviewState state)
    {
        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
