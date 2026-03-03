using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Chummer.Avalonia;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Overview;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Presentation;

[TestClass]
public class CharacterOverviewViewModelAdapterTests
{
    [TestMethod]
    public async Task InitializeAsync_delegates_to_presenter()
    {
        var presenter = new FakeCharacterOverviewPresenter();
        using var adapter = new CharacterOverviewViewModelAdapter(presenter);

        await adapter.InitializeAsync(CancellationToken.None);

        Assert.AreEqual(1, presenter.InitializeCalls);
    }

    [TestMethod]
    public async Task LoadAsync_delegates_to_presenter()
    {
        var presenter = new FakeCharacterOverviewPresenter();
        using var adapter = new CharacterOverviewViewModelAdapter(presenter);

        await adapter.LoadAsync(new CharacterWorkspaceId("ws-load"), CancellationToken.None);

        Assert.AreEqual("ws-load", presenter.LoadedWorkspaceId?.Value);
    }

    [TestMethod]
    public async Task SelectTabAsync_delegates_to_presenter()
    {
        var presenter = new FakeCharacterOverviewPresenter();
        using var adapter = new CharacterOverviewViewModelAdapter(presenter);

        await adapter.SelectTabAsync("tab-info", CancellationToken.None);

        Assert.AreEqual("tab-info", presenter.SelectedTabId);
    }

    [TestMethod]
    public async Task ImportAsync_delegates_to_presenter()
    {
        var presenter = new FakeCharacterOverviewPresenter();
        using var adapter = new CharacterOverviewViewModelAdapter(presenter);

        await adapter.ImportAsync(Encoding.UTF8.GetBytes("<character />"), CancellationToken.None);

        Assert.AreEqual("<character />", presenter.ImportedContent);
    }

    [TestMethod]
    public void Updated_event_is_raised_when_presenter_state_changes()
    {
        var presenter = new FakeCharacterOverviewPresenter();
        using var adapter = new CharacterOverviewViewModelAdapter(presenter);
        int updatedCount = 0;
        adapter.Updated += (_, _) => updatedCount++;

        presenter.Publish(CharacterOverviewState.Empty with { WorkspaceId = new CharacterWorkspaceId("ws-state") });

        Assert.AreEqual(1, updatedCount);
        Assert.AreEqual("ws-state", adapter.State.WorkspaceId?.Value);
    }

    [TestMethod]
    public void Dispose_unsubscribes_from_presenter_events()
    {
        var presenter = new FakeCharacterOverviewPresenter();
        var adapter = new CharacterOverviewViewModelAdapter(presenter);
        int updatedCount = 0;
        adapter.Updated += (_, _) => updatedCount++;
        adapter.Dispose();

        presenter.Publish(CharacterOverviewState.Empty with { WorkspaceId = new CharacterWorkspaceId("ws-after-dispose") });

        Assert.AreEqual(0, updatedCount);
    }
}
