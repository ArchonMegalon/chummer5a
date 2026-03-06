internal static class PortalBooleanResolver
{
    public static bool ResolveBoolean(string? configuredValue, string? environmentValue)
    {
        string? raw = configuredValue ?? environmentValue;
        return bool.TryParse(raw, out bool parsed) && parsed;
    }
}
