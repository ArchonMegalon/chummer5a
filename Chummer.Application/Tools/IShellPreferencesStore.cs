using Chummer.Contracts.Presentation;

namespace Chummer.Application.Tools;

public interface IShellPreferencesStore
{
    ShellPreferences Load();

    void Save(ShellPreferences preferences);
}
