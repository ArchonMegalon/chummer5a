using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Chummer.Blazor.Components.Layout;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation.Overview;
using Chummer.Presentation.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Presentation;

[TestClass]
public sealed class DesktopShellDownloadDispatchTests
{
    [TestMethod]
    public void OnAfterRenderAsync_dispatches_pending_download_once_per_version()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        FakeCharacterOverviewPresenter presenter = new();
        presenter.Publish(CreateOverviewState(CreateReceipt("ws-1"), 1));
        TrackingShellPresenter shellPresenter = new(ShellState.Empty);
        RegisterDesktopShellServices(context, presenter, shellPresenter);

        IRenderedComponent<DesktopShell> cut = context.Render<DesktopShell>();

        cut.WaitForAssertion(() => Assert.AreEqual(1, DownloadInvocationCount(context)));
        var firstInvocation = context.JSInterop.Invocations
            .First(invocation => string.Equals(invocation.Identifier, "chummerDownloads.downloadBase64", StringComparison.Ordinal));
        Assert.AreEqual("ws-1.chum5", firstInvocation.Arguments[0]?.ToString());

        presenter.Publish(CreateOverviewState(CreateReceipt("ws-1"), 1));
        cut.WaitForAssertion(() => Assert.AreEqual(1, DownloadInvocationCount(context)));

        presenter.Publish(CreateOverviewState(CreateReceipt("ws-1"), 2));
        cut.WaitForAssertion(() => Assert.AreEqual(2, DownloadInvocationCount(context)));
    }

    [TestMethod]
    public void OnAfterRenderAsync_does_not_dispatch_when_download_is_missing()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        FakeCharacterOverviewPresenter presenter = new();
        presenter.Publish(CreateOverviewState(receipt: null, version: 0));
        TrackingShellPresenter shellPresenter = new(ShellState.Empty);
        RegisterDesktopShellServices(context, presenter, shellPresenter);

        IRenderedComponent<DesktopShell> cut = context.Render<DesktopShell>();
        cut.WaitForAssertion(() => Assert.AreEqual(0, DownloadInvocationCount(context)));
    }

    private static int DownloadInvocationCount(BunitContext context)
    {
        return context.JSInterop.Invocations.Count(invocation =>
            string.Equals(invocation.Identifier, "chummerDownloads.downloadBase64", StringComparison.Ordinal));
    }

    private static CharacterOverviewState CreateOverviewState(WorkspaceDownloadReceipt? receipt, long version)
    {
        return CharacterOverviewState.Empty with
        {
            PendingDownload = receipt,
            PendingDownloadVersion = version
        };
    }

    private static WorkspaceDownloadReceipt CreateReceipt(string workspaceId)
    {
        return new WorkspaceDownloadReceipt(
            Id: new CharacterWorkspaceId(workspaceId),
            Format: WorkspaceDocumentFormat.Chum5Xml,
            ContentBase64: Convert.ToBase64String(Encoding.UTF8.GetBytes("<character/>")),
            FileName: $"{workspaceId}.chum5",
            DocumentLength: 12,
            RulesetId: "sr5");
    }

    private static void RegisterDesktopShellServices(
        BunitContext context,
        ICharacterOverviewPresenter presenter,
        IShellPresenter shellPresenter)
    {
        context.Services.AddSingleton(presenter);
        context.Services.AddSingleton(shellPresenter);
        context.Services.AddSingleton<ICommandAvailabilityEvaluator, DefaultCommandAvailabilityEvaluator>();
        context.Services.AddSingleton<IRulesetPlugin, Sr5RulesetPlugin>();
        context.Services.AddSingleton<IRulesetPluginRegistry, RulesetPluginRegistry>();
        context.Services.AddSingleton<IRulesetShellCatalogResolver, RulesetShellCatalogResolverService>();
    }

    private sealed class TrackingShellPresenter : IShellPresenter
    {
        public TrackingShellPresenter(ShellState state)
        {
            State = state;
        }

        public ShellState State { get; private set; }

        public event EventHandler? StateChanged;

        public Task InitializeAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task ExecuteCommandAsync(string commandId, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task SelectTabAsync(string tabId, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task ToggleMenuAsync(string menuId, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task SetPreferredRulesetAsync(string rulesetId, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task SyncWorkspaceContextAsync(CharacterWorkspaceId? activeWorkspaceId, CancellationToken ct)
        {
            State = State with { ActiveWorkspaceId = activeWorkspaceId };
            StateChanged?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }
    }
}
