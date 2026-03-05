using Chummer.Infrastructure.DependencyInjection;
using Chummer.Presentation;
using Chummer.Rulesets.Hosting;
using Chummer.Rulesets.Sr5;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chummer.Desktop.Runtime;

public static class ServiceCollectionDesktopRuntimeExtensions
{
    private const string ClientModeEnvironmentVariable = "CHUMMER_CLIENT_MODE";
    private const string LegacyDesktopClientModeEnvironmentVariable = "CHUMMER_DESKTOP_CLIENT_MODE";
    private const string ApiBaseUrlEnvironmentVariable = "CHUMMER_API_BASE_URL";
    private const string ApiKeyEnvironmentVariable = "CHUMMER_API_KEY";

    public static IServiceCollection AddChummerLocalRuntimeClient(
        this IServiceCollection services,
        string baseDirectory,
        string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.RemoveAll<IChummerClient>();

        if (UseHttpClientMode())
        {
            services.AddRulesetInfrastructure();
            services.AddSr5Ruleset();
            services.TryAddSingleton(CreateApiHttpClient());
            services.TryAddSingleton<IChummerClient, HttpChummerClient>();
            return services;
        }

        services.AddChummerHeadlessCore(baseDirectory, currentDirectory);
        services.TryAddSingleton<IChummerClient, InProcessChummerClient>();
        return services;
    }

    [Obsolete("Use AddChummerLocalRuntimeClient instead.")]
    public static IServiceCollection AddChummerDesktopRuntimeClient(
        this IServiceCollection services,
        string baseDirectory,
        string currentDirectory)
        => AddChummerLocalRuntimeClient(services, baseDirectory, currentDirectory);

    private static bool UseHttpClientMode()
    {
        string? raw = Environment.GetEnvironmentVariable(ClientModeEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(raw))
        {
            raw = Environment.GetEnvironmentVariable(LegacyDesktopClientModeEnvironmentVariable);
        }

        return string.Equals(raw?.Trim(), "http", StringComparison.OrdinalIgnoreCase);
    }

    private static HttpClient CreateApiHttpClient()
    {
        HttpClient client = new()
        {
            BaseAddress = ResolveApiBaseAddress(),
            Timeout = TimeSpan.FromSeconds(20)
        };

        string? apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.Remove("X-Api-Key");
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        return client;
    }

    private static Uri ResolveApiBaseAddress()
    {
        string? configured = Environment.GetEnvironmentVariable(ApiBaseUrlEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException(
                $"Set {ApiBaseUrlEnvironmentVariable} when {ClientModeEnvironmentVariable}=http (legacy: {LegacyDesktopClientModeEnvironmentVariable}=http).");
        }

        if (!Uri.TryCreate(configured, UriKind.Absolute, out Uri? uri))
        {
            throw new InvalidOperationException($"Invalid {ApiBaseUrlEnvironmentVariable}: '{configured}'");
        }

        return uri;
    }
}
