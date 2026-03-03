using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Overview;

namespace Chummer.Blazor;

public sealed class CharacterOverviewStateBridge : IDisposable
{
    private readonly ICharacterOverviewPresenter _presenter;
    private readonly Action<CharacterOverviewState> _onStateChanged;

    public CharacterOverviewStateBridge(
        ICharacterOverviewPresenter presenter,
        Action<CharacterOverviewState> onStateChanged)
    {
        _presenter = presenter;
        _onStateChanged = onStateChanged;
        _presenter.StateChanged += HandlePresenterStateChanged;
    }

    public CharacterOverviewState Current => _presenter.State;

    public Task LoadAsync(CharacterWorkspaceId workspaceId, CancellationToken ct)
    {
        return _presenter.LoadAsync(workspaceId, ct);
    }

    public Task ImportAsync(string xml, CancellationToken ct)
    {
        return _presenter.ImportAsync(new WorkspaceImportDocument(xml), ct);
    }

    public void Dispose()
    {
        _presenter.StateChanged -= HandlePresenterStateChanged;
    }

    private void HandlePresenterStateChanged(object? sender, EventArgs args)
    {
        _onStateChanged(_presenter.State);
    }
}
