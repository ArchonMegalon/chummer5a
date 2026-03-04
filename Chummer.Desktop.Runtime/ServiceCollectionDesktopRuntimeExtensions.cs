using Chummer.Infrastructure.DependencyInjection;
using Chummer.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chummer.Desktop.Runtime;

public static class ServiceCollectionDesktopRuntimeExtensions
{
    private const string DesktopClientModeEnvironmentVariable = "CHUMMER_DESKTOP_CLIENT_MODE";
    private const string ApiBaseUrlEnvironmentVariable = "CHUMMER_API_BASE_URL";
    private const string ApiKeyEnvironmentVariable = "CHUMMER_API_KEY";

    public static IServiceCollection AddChummerDesktopRuntimeClient(
        this IServiceCollection services,
        string baseDirectory,
        string currentDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.RemoveAll<IChummerClient>();

        if (UseHttpClientMode())
        {
            services.TryAddSingleton(CreateApiHttpClient());
            services.TryAddSingleton<IChummerClient, HttpChummerClient>();
            return services;
        }

        services.AddChummerHeadlessCore(baseDirectory, currentDirectory);
        services.TryAddSingleton<IChummerClient, InProcessChummerClient>();
        return services;
    }

    private static bool UseHttpClientMode()
    {
        string? raw = Environment.GetEnvironmentVariable(DesktopClientModeEnvironmentVariable);
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
            configured = "http://127.0.0.1:8088";
        }

        if (!Uri.TryCreate(configured, UriKind.Absolute, out Uri? uri))
        {
            throw new InvalidOperationException($"Invalid {ApiBaseUrlEnvironmentVariable}: '{configured}'");
        }

        return uri;
    }
}
