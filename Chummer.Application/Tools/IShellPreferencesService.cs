using Chummer.Contracts.Presentation;

namespace Chummer.Application.Tools;

public interface IShellPreferencesService
{
    ShellPreferences Load();

    void Save(ShellPreferences preferences);
}
