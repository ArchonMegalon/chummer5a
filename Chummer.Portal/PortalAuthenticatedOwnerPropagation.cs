using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Chummer.Contracts.Owners;
using Microsoft.AspNetCore.Http;

public static class PortalAuthenticatedOwnerPropagation
{
    public static void Apply(HttpContext context, string? sharedKey)
    {
        ArgumentNullException.ThrowIfNull(context);

        IHeaderDictionary headers = context.Request.Headers;
        headers.Remove(PortalOwnerPropagationContract.OwnerHeaderName);
        headers.Remove(PortalOwnerPropagationContract.TimestampHeaderName);
        headers.Remove(PortalOwnerPropagationContract.SignatureHeaderName);

        if (!ShouldProjectOwner(context.Request.Path) || string.IsNullOrWhiteSpace(sharedKey))
        {
            return;
        }

        string? owner = ResolveAuthenticatedOwner(context.User);
        if (string.IsNullOrWhiteSpace(owner))
        {
            return;
        }

        string normalizedOwner = new OwnerScope(owner).NormalizedValue;
        if (string.IsNullOrWhiteSpace(normalizedOwner))
        {
            return;
        }

        string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        headers[PortalOwnerPropagationContract.OwnerHeaderName] = normalizedOwner;
        headers[PortalOwnerPropagationContract.TimestampHeaderName] = timestamp;
        headers[PortalOwnerPropagationContract.SignatureHeaderName] = CreateSignature(normalizedOwner, timestamp, sharedKey);
    }

    public static string CreateSignature(string normalizedOwner, string unixTimestamp, string sharedKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sharedKey);

        byte[] keyBytes = Encoding.UTF8.GetBytes(sharedKey.Trim());
        byte[] payloadBytes = Encoding.UTF8.GetBytes(
            PortalOwnerPropagationContract.BuildSignaturePayload(normalizedOwner, unixTimestamp));

        using HMACSHA256 hmac = new(keyBytes);
        return Convert.ToHexString(hmac.ComputeHash(payloadBytes));
    }

    private static bool ShouldProjectOwner(PathString path)
    {
        return path.StartsWithSegments("/api", StringComparison.Ordinal)
            || path.StartsWithSegments("/docs", StringComparison.Ordinal)
            || path.StartsWithSegments("/openapi", StringComparison.Ordinal);
    }

    private static string? ResolveAuthenticatedOwner(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value
            ?? principal.Identity?.Name;
    }
}
