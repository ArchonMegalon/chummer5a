using Chummer.Contracts.Owners;

namespace Chummer.Infrastructure.Files;

internal static class SettingsOwnerScope
{
    private const string GlobalSettingsScope = "global";

    public static string Resolve(OwnerScope owner)
    {
        if (owner.IsLocalSingleUser || string.IsNullOrWhiteSpace(owner.NormalizedValue))
        {
            return GlobalSettingsScope;
        }

        return $"owner-{Uri.EscapeDataString(owner.NormalizedValue)}";
    }
}
