using Chummer.Contracts.Presentation;

namespace Chummer.Application.Tools;

public interface IShellPreferencesService
{
    ShellUserPreferences Load();

    void Save(ShellUserPreferences preferences);
}
