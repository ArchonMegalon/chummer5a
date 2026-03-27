using System.Text.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

internal static class PortalDownloadsService
{
    private static readonly Regex LocalArtifactPattern = new(
        @"^chummer-(?<app>avalonia|blazor-desktop)-(?<rid>.+?)(?:-(?<flavor>installer|portable))?\.(?<ext>zip|tar\.gz|exe|deb|dmg)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static DownloadReleaseManifest LoadReleaseManifest(string manifestPath, string releaseFilesPath, string fallbackDownloadsUrl)
    {
        bool hasFallbackSource = HasConfiguredFallbackSource(fallbackDownloadsUrl);
        ManifestLoadState manifestState = TryLoadManifest(manifestPath, hasFallbackSource);
        DownloadReleaseManifest? parsedManifest = manifestState.Manifest;
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
                Downloads: discoveredDownloads,
                Source: "local-files",
                Status: "published",
                Message: "Using locally discovered desktop artifacts from portal storage.",
                HasFallbackSource: hasFallbackSource);
        }

        if (parsedManifest is not null)
        {
            return parsedManifest;
        }

        if (manifestState.Exists && !manifestState.ParseSucceeded)
        {
            return BuildManifestErrorManifest(hasFallbackSource);
        }

        if (hasFallbackSource)
        {
            return BuildFallbackManifest(fallbackDownloadsUrl);
        }

        return BuildMissingManifest();
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
            Version: "unpublished",
            Channel: "docker",
            PublishedAt: DateTimeOffset.UtcNow,
            Downloads: downloads,
            Source: "fallback",
            Status: "fallback-source",
            Message: "Portal is using the configured fallback downloads source because no self-hosted manifest or local artifacts were found.",
            HasFallbackSource: true);
    }

    private static DownloadReleaseManifest BuildMissingManifest()
    {
        return new DownloadReleaseManifest(
            Version: "unpublished",
            Channel: "docker",
            PublishedAt: DateTimeOffset.UtcNow,
            Downloads: Array.Empty<DownloadArtifact>(),
            Source: "manifest",
            Status: "manifest-missing",
            Message: "Release manifest is missing and no local artifacts were discovered. Verify the portal downloads mount or proxy target.",
            HasFallbackSource: false);
    }

    private static DownloadReleaseManifest BuildManifestErrorManifest(bool hasFallbackSource)
    {
        return new DownloadReleaseManifest(
            Version: "unpublished",
            Channel: "docker",
            PublishedAt: DateTimeOffset.UtcNow,
            Downloads: Array.Empty<DownloadArtifact>(),
            Source: "manifest",
            Status: "manifest-error",
            Message: "Release manifest exists but could not be parsed. Verify the deployed releases.json payload.",
            HasFallbackSource: hasFallbackSource);
    }

    private static ManifestLoadState TryLoadManifest(string manifestPath, bool hasFallbackSource)
    {
        if (!File.Exists(manifestPath))
        {
            return new ManifestLoadState(null, Exists: false, ParseSucceeded: false);
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
                return new ManifestLoadState(null, Exists: true, ParseSucceeded: false);
            }

            DownloadReleaseManifest normalizedManifest = manifest with
            {
                Downloads = manifest.Downloads ?? Array.Empty<DownloadArtifact>(),
                Source = "manifest",
                Status = ResolveManifestStatus(manifest),
                Message = BuildManifestMessage(manifest),
                HasFallbackSource = hasFallbackSource
            };
            return new ManifestLoadState(normalizedManifest, Exists: true, ParseSucceeded: true);
        }
        catch
        {
            return new ManifestLoadState(null, Exists: true, ParseSucceeded: false);
        }
    }

    private static string ResolveManifestStatus(DownloadReleaseManifest manifest)
    {
        if (manifest.Downloads.Count > 0)
        {
            return "published";
        }

        return string.Equals(manifest.Version, "unpublished", StringComparison.OrdinalIgnoreCase)
            ? "unpublished"
            : "manifest-empty";
    }

    private static string? BuildManifestMessage(DownloadReleaseManifest manifest)
    {
        if (manifest.Downloads.Count > 0)
        {
            return null;
        }

        if (string.Equals(manifest.Version, "unpublished", StringComparison.OrdinalIgnoreCase))
        {
            return "No published desktop builds yet. Run desktop-downloads workflow and deploy the generated bundle.";
        }

        return "Release manifest is present but contains no downloadable artifacts. Verify the deployment bundle and manifest generation step.";
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

                string app = match.Groups["app"].Value;
                string rid = match.Groups["rid"].Value;
                string ext = match.Groups["ext"].Value;
                string flavor = NormalizeFlavor(match.Groups["flavor"].Value, ext);
                if (!IsPublicShelfArtifact(rid))
                {
                    continue;
                }

                artifacts.Add(new DownloadArtifact(
                    Id: $"{app}-{rid}-{flavor}",
                    Platform: BuildPlatformLabel(app, rid, flavor),
                    Url: $"/downloads/{relativePath}",
                    Sha256: ComputeSha256(filePath),
                    SizeBytes: new FileInfo(filePath).Length,
                    Format: ext,
                    Flavor: flavor,
                    App: app,
                    Rid: rid,
                    Head: ResolveHead(app),
                    Recommended: string.Equals(app, "avalonia", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(flavor, "installer", StringComparison.OrdinalIgnoreCase)));
            }
        }

        return artifacts
            .OrderBy(artifact => artifact.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static string BuildPlatformLabel(string app, string rid, string flavor)
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

        string flavorLabel = flavor.ToLowerInvariant() switch
        {
            "installer" => "Installer",
            "portable" => "Portable",
            "archive" => "Archive",
            _ => flavor
        };

        return $"{appLabel} {ridLabel} {flavorLabel}";
    }

    private static string NormalizeFlavor(string rawFlavor, string ext)
    {
        if (!string.IsNullOrWhiteSpace(rawFlavor))
        {
            return rawFlavor.ToLowerInvariant();
        }

        string normalizedExt = ext.ToLowerInvariant();
        if (string.Equals(normalizedExt, "zip", StringComparison.Ordinal) || string.Equals(normalizedExt, "tar.gz", StringComparison.Ordinal))
        {
            return "archive";
        }

        if (string.Equals(normalizedExt, "deb", StringComparison.Ordinal) || string.Equals(normalizedExt, "dmg", StringComparison.Ordinal))
        {
            return "installer";
        }

        return "portable";
    }

    private static string ResolveHead(string app)
    {
        return string.Equals(app, "avalonia", StringComparison.OrdinalIgnoreCase) ? "flagship" : "fallback";
    }

    private static bool IsPublicShelfArtifact(string rid)
    {
        if (!rid.StartsWith("osx", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string? rawValue = Environment.GetEnvironmentVariable("CHUMMER_MACOS_PUBLIC_SHELF_ENABLED");
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        return rawValue.Trim().Equals("1", StringComparison.OrdinalIgnoreCase)
            || rawValue.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
            || rawValue.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase)
            || rawValue.Trim().Equals("on", StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        byte[] hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed record ManifestLoadState(
        DownloadReleaseManifest? Manifest,
        bool Exists,
        bool ParseSucceeded);
}
