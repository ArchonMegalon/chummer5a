using Microsoft.AspNetCore.Http;

internal static class PortalProxyUtils
{
    public static string NormalizeProxyAddress(string baseUrl)
    {
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? absoluteBase))
        {
            string normalized = absoluteBase.ToString();
            return normalized.EndsWith("/", StringComparison.Ordinal) ? normalized : $"{normalized}/";
        }

        return baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : $"{baseUrl}/";
    }

    public static IReadOnlyList<IReadOnlyDictionary<string, string>>? BuildApiRouteTransforms(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        return new[]
        {
            (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["RequestHeader"] = "X-Api-Key",
                ["Set"] = apiKey
            }
        };
    }

    public static IReadOnlyList<IReadOnlyDictionary<string, string>>? BuildRouteTransforms(
        IReadOnlyList<IReadOnlyDictionary<string, string>>? apiRouteTransforms,
        string? pathRemovePrefix = null)
    {
        List<IReadOnlyDictionary<string, string>> transforms = new();

        if (!string.IsNullOrWhiteSpace(pathRemovePrefix))
        {
            transforms.Add(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["PathRemovePrefix"] = pathRemovePrefix
            });
        }

        if (apiRouteTransforms is not null)
        {
            transforms.AddRange(apiRouteTransforms);
        }

        return transforms.Count == 0 ? null : transforms;
    }

    public static string ComposeRedirect(string baseUrl, string? path, QueryString queryString)
    {
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? absoluteBase))
        {
            string cleanPath = string.IsNullOrWhiteSpace(path) ? string.Empty : path.TrimStart('/');
            Uri redirected = new(absoluteBase, cleanPath);
            return $"{redirected}{queryString}";
        }

        string normalizedBase = baseUrl.TrimEnd('/');
        string suffix = string.IsNullOrWhiteSpace(path) ? string.Empty : $"/{path.TrimStart('/')}";
        return $"{normalizedBase}{suffix}{queryString}";
    }
}
