using Chummer.Contracts.Characters;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Overview;

public sealed class WorkspaceSessionActivationService : IWorkspaceSessionActivationService
{
    public WorkspaceSessionState Activate(
        IWorkspaceSessionPresenter sessionPresenter,
        CharacterWorkspaceId workspaceId,
        CharacterProfileSection? profile,
        WorkspaceSessionState? sessionSeed,
        bool updateSession)
    {
        if (sessionSeed is null && updateSession)
        {
            return sessionPresenter.Open(workspaceId, profile);
        }

        WorkspaceSessionState session = sessionPresenter.Switch(workspaceId);
        if (session.ActiveWorkspaceId is null
            || !string.Equals(session.ActiveWorkspaceId.Value.Value, workspaceId.Value, StringComparison.Ordinal))
        {
            session = sessionPresenter.Open(workspaceId, profile);
        }

        return session;
    }
}
