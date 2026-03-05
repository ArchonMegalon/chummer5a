using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Chummer.Contracts.Rulesets;
using Chummer.Desktop.Runtime;
using Chummer.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class ServiceCollectionDesktopRuntimeExtensionsTests
{
    private static readonly object EnvironmentLock = new();

    [TestMethod]
    public void Default_mode_registers_inprocess_client()
    {
        lock (EnvironmentLock)
        {
            string root = CreateTempDirectory();
            try
            {
                ApplyEnvironment(mode: null, baseUrl: null, apiKey: null, () =>
                {
                    var services = new ServiceCollection();
                    services.AddChummerLocalRuntimeClient(root, root);

                    using ServiceProvider provider = services.BuildServiceProvider();
                    IChummerClient client = provider.GetRequiredService<IChummerClient>();
                    IReadOnlyList<IRulesetPlugin> plugins = provider.GetServices<IRulesetPlugin>().ToArray();

                    Assert.IsInstanceOfType<InProcessChummerClient>(client);
                    Assert.IsTrue(plugins.Any(plugin => string.Equals(plugin.Id.NormalizedValue, RulesetDefaults.Sr5, StringComparison.Ordinal)));
                });
            }
            finally
            {
                DeleteTempDirectory(root);
            }
        }
    }

    [TestMethod]
    public void Http_mode_requires_explicit_api_base_url()
    {
        lock (EnvironmentLock)
        {
            string root = CreateTempDirectory();
            try
            {
                ApplyEnvironment(mode: "http", baseUrl: null, apiKey: null, () =>
                {
                    var services = new ServiceCollection();

                    InvalidOperationException? ex = null;
                    try
                    {
                        services.AddChummerLocalRuntimeClient(root, root);
                    }
                    catch (InvalidOperationException captured)
                    {
                        ex = captured;
                    }

                    Assert.IsNotNull(ex);
                    StringAssert.Contains(ex.Message, "CHUMMER_API_BASE_URL");
                });
            }
            finally
            {
                DeleteTempDirectory(root);
            }
        }
    }

    [TestMethod]
    public void Http_mode_registers_http_client_and_api_key_header_when_configured()
    {
        lock (EnvironmentLock)
        {
            string root = CreateTempDirectory();
            try
            {
                ApplyEnvironment(mode: "http", baseUrl: "https://api.example.invalid/", apiKey: "test-key", () =>
                {
                    var services = new ServiceCollection();
                    services.AddChummerLocalRuntimeClient(root, root);

                    using ServiceProvider provider = services.BuildServiceProvider();
                    IChummerClient client = provider.GetRequiredService<IChummerClient>();
                    HttpClient httpClient = provider.GetRequiredService<HttpClient>();

                    Assert.IsInstanceOfType<HttpChummerClient>(client);
                    Assert.IsNotNull(httpClient.BaseAddress);
                    Assert.AreEqual("https://api.example.invalid/", httpClient.BaseAddress!.ToString());
                    Assert.IsTrue(httpClient.DefaultRequestHeaders.Contains("X-Api-Key"));
                    CollectionAssert.AreEqual(
                        new[] { "test-key" },
                        new List<string>(httpClient.DefaultRequestHeaders.GetValues("X-Api-Key")));
                });
            }
            finally
            {
                DeleteTempDirectory(root);
            }
        }
    }

    [TestMethod]
    public void Legacy_desktop_client_mode_environment_variable_remains_supported()
    {
        lock (EnvironmentLock)
        {
            string root = CreateTempDirectory();
            try
            {
                ApplyEnvironment(mode: null, legacyMode: "http", baseUrl: "https://legacy.example.invalid/", apiKey: null, () =>
                {
                    var services = new ServiceCollection();
                    services.AddChummerLocalRuntimeClient(root, root);

                    using ServiceProvider provider = services.BuildServiceProvider();
                    IChummerClient client = provider.GetRequiredService<IChummerClient>();
                    HttpClient httpClient = provider.GetRequiredService<HttpClient>();

                    Assert.IsInstanceOfType<HttpChummerClient>(client);
                    Assert.IsNotNull(httpClient.BaseAddress);
                    Assert.AreEqual("https://legacy.example.invalid/", httpClient.BaseAddress!.ToString());
                });
            }
            finally
            {
                DeleteTempDirectory(root);
            }
        }
    }

    private static void ApplyEnvironment(string? mode, string? baseUrl, string? apiKey, Action action)
        => ApplyEnvironment(mode, legacyMode: mode, baseUrl, apiKey, action);

    private static void ApplyEnvironment(string? mode, string? legacyMode, string? baseUrl, string? apiKey, Action action)
    {
        string? previousMode = Environment.GetEnvironmentVariable("CHUMMER_CLIENT_MODE");
        string? previousLegacyMode = Environment.GetEnvironmentVariable("CHUMMER_DESKTOP_CLIENT_MODE");
        string? previousBaseUrl = Environment.GetEnvironmentVariable("CHUMMER_API_BASE_URL");
        string? previousApiKey = Environment.GetEnvironmentVariable("CHUMMER_API_KEY");

        try
        {
            Environment.SetEnvironmentVariable("CHUMMER_CLIENT_MODE", mode);
            Environment.SetEnvironmentVariable("CHUMMER_DESKTOP_CLIENT_MODE", legacyMode);
            Environment.SetEnvironmentVariable("CHUMMER_API_BASE_URL", baseUrl);
            Environment.SetEnvironmentVariable("CHUMMER_API_KEY", apiKey);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable("CHUMMER_CLIENT_MODE", previousMode);
            Environment.SetEnvironmentVariable("CHUMMER_DESKTOP_CLIENT_MODE", previousLegacyMode);
            Environment.SetEnvironmentVariable("CHUMMER_API_BASE_URL", previousBaseUrl);
            Environment.SetEnvironmentVariable("CHUMMER_API_KEY", previousApiKey);
        }
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "chummer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTempDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failures in tests.
        }
    }
}
