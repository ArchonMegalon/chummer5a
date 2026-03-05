using Chummer.Contracts.Presentation;

namespace Chummer.Application.Tools;

public interface IShellSessionService
{
    ShellSessionState Load();

    void Save(ShellSessionState session);
}
