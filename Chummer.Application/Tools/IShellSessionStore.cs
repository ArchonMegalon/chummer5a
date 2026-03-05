using Chummer.Contracts.Presentation;

namespace Chummer.Application.Tools;

public interface IShellSessionStore
{
    ShellSessionState Load();

    void Save(ShellSessionState session);
}
