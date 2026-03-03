using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Overview;

namespace Chummer.Avalonia;

public sealed class CharacterOverviewViewModelAdapter : IDisposable
{
    private readonly ICharacterOverviewPresenter _presenter;

    public CharacterOverviewViewModelAdapter(ICharacterOverviewPresenter presenter)
    {
        _presenter = presenter;
        _presenter.StateChanged += HandlePresenterStateChanged;
    }

    public event EventHandler? Updated;

    public CharacterOverviewState State => _presenter.State;

    public Task InitializeAsync(CancellationToken ct)
    {
        return _presenter.InitializeAsync(ct);
    }

    public Task LoadAsync(CharacterWorkspaceId workspaceId, CancellationToken ct)
    {
        return _presenter.LoadAsync(workspaceId, ct);
    }

    public Task SelectTabAsync(string tabId, CancellationToken ct)
    {
        return _presenter.SelectTabAsync(tabId, ct);
    }

    public Task ImportAsync(byte[] documentBytes, CancellationToken ct)
    {
        return _presenter.ImportAsync(WorkspaceImportDocument.FromUtf8Bytes(documentBytes), ct);
    }

    public void Dispose()
    {
        _presenter.StateChanged -= HandlePresenterStateChanged;
    }

    private void HandlePresenterStateChanged(object? sender, EventArgs args)
    {
        Updated?.Invoke(this, EventArgs.Empty);
    }
}
