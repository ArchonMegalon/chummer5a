using Chummer.Contracts.Presentation;

namespace Chummer.Application.Tools;

public interface IShellPreferencesStore
{
    ShellUserPreferences Load();

    void Save(ShellUserPreferences preferences);
}
