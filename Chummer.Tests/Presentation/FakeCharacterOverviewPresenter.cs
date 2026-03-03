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

    public string? ImportedXml { get; private set; }

    public UpdateWorkspaceMetadata? UpdatedMetadata { get; private set; }

    public int SaveCalls { get; private set; }

    public Task ImportAsync(string xml, CancellationToken ct)
    {
        ImportedXml = xml;
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
