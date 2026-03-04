using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Overview;

public sealed partial class CharacterOverviewPresenter
{
    private async Task LoadSectionAsync(string sectionId, string? tabId, string? actionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sectionId))
        {
            Publish(State with { Error = "Section id is required." });
            return;
        }

        if (_currentWorkspace is null)
        {
            Publish(State with { Error = "No workspace loaded." });
            return;
        }

        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            WorkspaceSectionRenderResult section = await _workspaceSectionRenderer.RenderSectionAsync(
                _client,
                _currentWorkspace.Value,
                sectionId,
                tabId,
                actionId,
                State.ActiveTabId,
                State.ActiveActionId,
                ct);
            Publish(State with
            {
                IsBusy = false,
                Error = null,
                ActiveTabId = section.ActiveTabId,
                ActiveActionId = section.ActiveActionId,
                ActiveSectionId = section.ActiveSectionId,
                ActiveSectionJson = section.ActiveSectionJson,
                ActiveSectionRows = section.ActiveSectionRows
            });
            CaptureWorkspaceView();
        }
        catch (Exception ex)
        {
            Publish(State with
            {
                IsBusy = false,
                Error = ex.Message
            });
        }
    }

    private async Task RenderSummaryAction(WorkspaceSurfaceActionDefinition action, CancellationToken ct)
    {
        if (_currentWorkspace is null)
        {
            Publish(State with { Error = "No workspace loaded." });
            return;
        }

        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            WorkspaceSectionRenderResult summary = await _workspaceSectionRenderer.RenderSummaryAsync(
                _client,
                _currentWorkspace.Value,
                action,
                ct);
            Publish(State with
            {
                IsBusy = false,
                Error = null,
                ActiveTabId = summary.ActiveTabId,
                ActiveActionId = summary.ActiveActionId,
                ActiveSectionId = summary.ActiveSectionId,
                ActiveSectionJson = summary.ActiveSectionJson,
                ActiveSectionRows = summary.ActiveSectionRows
            });
            CaptureWorkspaceView();
        }
        catch (Exception ex)
        {
            Publish(State with
            {
                IsBusy = false,
                Error = ex.Message
            });
        }
    }

    private async Task RenderValidateAction(WorkspaceSurfaceActionDefinition action, CancellationToken ct)
    {
        if (_currentWorkspace is null)
        {
            Publish(State with { Error = "No workspace loaded." });
            return;
        }

        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            WorkspaceSectionRenderResult validation = await _workspaceSectionRenderer.RenderValidationAsync(
                _client,
                _currentWorkspace.Value,
                action,
                ct);
            Publish(State with
            {
                IsBusy = false,
                Error = null,
                ActiveTabId = validation.ActiveTabId,
                ActiveActionId = validation.ActiveActionId,
                ActiveSectionId = validation.ActiveSectionId,
                ActiveSectionJson = validation.ActiveSectionJson,
                ActiveSectionRows = validation.ActiveSectionRows
            });
            CaptureWorkspaceView();
        }
        catch (Exception ex)
        {
            Publish(State with
            {
                IsBusy = false,
                Error = ex.Message
            });
        }
    }

    private void CaptureWorkspaceView()
    {
        if (_currentWorkspace is null)
            return;

        _workspaceViews[_currentWorkspace.Value.Value] = new WorkspaceViewState(
            ActiveTabId: State.ActiveTabId,
            ActiveActionId: State.ActiveActionId,
            ActiveSectionId: State.ActiveSectionId,
            ActiveSectionJson: State.ActiveSectionJson,
            ActiveSectionRows: State.ActiveSectionRows.ToArray(),
            HasSavedWorkspace: State.HasSavedWorkspace);
    }

    private WorkspaceViewState? RestoreWorkspaceView(CharacterWorkspaceId id)
    {
        return _workspaceViews.TryGetValue(id.Value, out WorkspaceViewState? view)
            ? view
            : null;
    }
}
