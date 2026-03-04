internal static class PortalSettingsResolver
{
    public static string ResolveSetting(IConfiguration configuration, string key, string envVar, string fallback)
    {
        string? configured = configuration[key];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        string? environment = Environment.GetEnvironmentVariable(envVar);
        if (!string.IsNullOrWhiteSpace(environment))
        {
            return environment;
        }

        return fallback;
    }
}
