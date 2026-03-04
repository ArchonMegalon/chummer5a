using System.Text.Json;

internal static class PortalDownloadsService
{
    public static DownloadReleaseManifest LoadReleaseManifest(string manifestPath, string fallbackDownloadsUrl)
    {
        if (!File.Exists(manifestPath))
        {
            return BuildFallbackManifest(fallbackDownloadsUrl);
        }

        try
        {
            string json = File.ReadAllText(manifestPath);
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };
            DownloadReleaseManifest? manifest = JsonSerializer.Deserialize<DownloadReleaseManifest>(json, options);
            if (manifest is null || string.IsNullOrWhiteSpace(manifest.Version))
            {
                return BuildFallbackManifest(fallbackDownloadsUrl);
            }

            return manifest with
            {
                Downloads = manifest.Downloads ?? Array.Empty<DownloadArtifact>()
            };
        }
        catch
        {
            return BuildFallbackManifest(fallbackDownloadsUrl);
        }
    }

    public static string ResolveManifestPath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
    }

    public static string ResolveReleaseFilesPath(string configuredPath, string manifestPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            if (Path.IsPathRooted(configuredPath))
            {
                return configuredPath;
            }

            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
        }

        string? fromManifest = Path.GetDirectoryName(manifestPath);
        if (!string.IsNullOrWhiteSpace(fromManifest))
        {
            return fromManifest;
        }

        return AppContext.BaseDirectory;
    }

    public static string? ResolveDownloadFilePath(string rootDirectory, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string rootPath = Path.GetFullPath(rootDirectory);
        string cleanedPath = path.TrimStart('/').Replace('\\', '/');
        if (cleanedPath.Contains("..", StringComparison.Ordinal))
        {
            return null;
        }

        string localPath = cleanedPath.Replace('/', Path.DirectorySeparatorChar);
        string candidatePath = Path.GetFullPath(Path.Combine(rootPath, localPath));
        if (!candidatePath.StartsWith(rootPath, StringComparison.Ordinal))
        {
            return null;
        }

        if (!File.Exists(candidatePath))
        {
            return null;
        }

        return candidatePath;
    }

    public static bool HasConfiguredFallbackSource(string fallbackDownloadsUrl)
    {
        if (string.IsNullOrWhiteSpace(fallbackDownloadsUrl))
        {
            return false;
        }

        string normalized = fallbackDownloadsUrl.Trim().TrimEnd('/');
        return !string.Equals(normalized, "/downloads", StringComparison.OrdinalIgnoreCase);
    }

    private static DownloadReleaseManifest BuildFallbackManifest(string fallbackDownloadsUrl)
    {
        IReadOnlyList<DownloadArtifact> downloads = Array.Empty<DownloadArtifact>();
        if (HasConfiguredFallbackSource(fallbackDownloadsUrl))
        {
            downloads =
            [
                new DownloadArtifact(
                    Id: "configured-fallback-source",
                    Platform: "Configured fallback source",
                    Url: fallbackDownloadsUrl,
                    Sha256: string.Empty)
            ];
        }

        return new DownloadReleaseManifest(
            Version: "nightly",
            Channel: "docker",
            PublishedAt: DateTimeOffset.UtcNow,
            Downloads: downloads);
    }
}
