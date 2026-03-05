using Chummer.Contracts.Presentation;

namespace Chummer.Application.Tools;

public sealed class ShellSessionService : IShellSessionService
{
    private readonly IShellSessionStore _store;

    public ShellSessionService(IShellSessionStore store)
    {
        _store = store;
    }

    public ShellSessionState Load()
    {
        ShellSessionState stored = _store.Load();
        return new ShellSessionState(
            ActiveWorkspaceId: NormalizeWorkspaceId(stored.ActiveWorkspaceId));
    }

    public void Save(ShellSessionState session)
    {
        ShellSessionState normalized = new(
            ActiveWorkspaceId: NormalizeWorkspaceId(session.ActiveWorkspaceId));
        _store.Save(normalized);
    }

    private static string? NormalizeWorkspaceId(string? workspaceId)
    {
        return string.IsNullOrWhiteSpace(workspaceId)
            ? null
            : workspaceId.Trim();
    }
}
