using System.Text.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

internal static class PortalDownloadsService
{
    private static readonly Regex LocalArtifactPattern = new(
        @"^chummer-(?<app>avalonia|blazor-desktop)-(?<rid>[^.]+)\.(?<ext>zip|tar\.gz)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static DownloadReleaseManifest LoadReleaseManifest(string manifestPath, string releaseFilesPath, string fallbackDownloadsUrl)
    {
        DownloadReleaseManifest? parsedManifest = TryParseManifest(manifestPath);
        if (parsedManifest is not null && parsedManifest.Downloads.Count > 0)
        {
            return parsedManifest;
        }

        IReadOnlyList<DownloadArtifact> discoveredDownloads = DiscoverLocalArtifacts(releaseFilesPath);
        if (discoveredDownloads.Count > 0)
        {
            string version = parsedManifest?.Version ?? "local-files";
            if (string.IsNullOrWhiteSpace(version) || string.Equals(version, "unpublished", StringComparison.OrdinalIgnoreCase))
            {
                version = "local-files";
            }

            return new DownloadReleaseManifest(
                Version: version,
                Channel: parsedManifest?.Channel ?? "docker",
                PublishedAt: parsedManifest?.PublishedAt ?? DateTimeOffset.UtcNow,
                Downloads: discoveredDownloads);
        }

        if (parsedManifest is not null)
        {
            return parsedManifest;
        }

        return BuildFallbackManifest(fallbackDownloadsUrl);
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

    private static DownloadReleaseManifest? TryParseManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return null;
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
                return null;
            }

            return manifest with
            {
                Downloads = manifest.Downloads ?? Array.Empty<DownloadArtifact>()
            };
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<DownloadArtifact> DiscoverLocalArtifacts(string releaseFilesPath)
    {
        if (string.IsNullOrWhiteSpace(releaseFilesPath))
        {
            return Array.Empty<DownloadArtifact>();
        }

        string root = Path.GetFullPath(releaseFilesPath);
        string filesSubdirectory = Path.Combine(root, "files");
        string[] candidateDirectories =
        [
            root,
            filesSubdirectory
        ];

        var artifacts = new List<DownloadArtifact>();
        foreach (string directory in candidateDirectories.Distinct(StringComparer.Ordinal))
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (string filePath in Directory.EnumerateFiles(directory, "chummer-*", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(filePath);
                Match match = LocalArtifactPattern.Match(fileName);
                if (!match.Success)
                {
                    continue;
                }

                string relativePath = Path.GetRelativePath(root, filePath).Replace('\\', '/');
                if (relativePath.StartsWith("..", StringComparison.Ordinal))
                {
                    continue;
                }

                artifacts.Add(new DownloadArtifact(
                    Id: $"{match.Groups["app"].Value}-{match.Groups["rid"].Value}",
                    Platform: BuildPlatformLabel(match.Groups["app"].Value, match.Groups["rid"].Value),
                    Url: $"/downloads/{relativePath}",
                    Sha256: ComputeSha256(filePath)));
            }
        }

        return artifacts
            .OrderBy(artifact => artifact.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static string BuildPlatformLabel(string app, string rid)
    {
        string appLabel = app.ToLowerInvariant() switch
        {
            "avalonia" => "Avalonia Desktop",
            "blazor-desktop" => "Blazor Desktop",
            _ => app
        };

        string ridLabel = rid.ToLowerInvariant() switch
        {
            "win-x64" => "Windows x64",
            "win-arm64" => "Windows ARM64",
            "linux-x64" => "Linux x64",
            "linux-arm64" => "Linux ARM64",
            "osx-arm64" => "macOS ARM64",
            "osx-x64" => "macOS x64",
            _ => rid
        };

        return $"{appLabel} {ridLabel}";
    }

    private static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        byte[] hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
