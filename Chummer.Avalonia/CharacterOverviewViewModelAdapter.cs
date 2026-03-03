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

    public Task LoadAsync(CharacterWorkspaceId workspaceId, CancellationToken ct)
    {
        return _presenter.LoadAsync(workspaceId, ct);
    }

    public Task ImportAsync(string xml, CancellationToken ct)
    {
        return _presenter.ImportAsync(xml, ct);
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
