using System.Globalization;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Chummer.Application.Owners;
using Chummer.Contracts.Owners;

namespace Chummer.Api.Owners;

public sealed class RequestOwnerContextAccessor : IOwnerContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string? _headerName;
    private readonly string? _portalOwnerSharedKey;
    private readonly TimeSpan _portalOwnerMaxAge;

    public RequestOwnerContextAccessor(
        IHttpContextAccessor httpContextAccessor,
        string? headerName = null,
        string? portalOwnerSharedKey = null,
        TimeSpan? portalOwnerMaxAge = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _headerName = string.IsNullOrWhiteSpace(headerName)
            ? null
            : headerName.Trim();
        _portalOwnerSharedKey = string.IsNullOrWhiteSpace(portalOwnerSharedKey)
            ? null
            : portalOwnerSharedKey.Trim();
        _portalOwnerMaxAge = portalOwnerMaxAge.GetValueOrDefault(
            TimeSpan.FromSeconds(PortalOwnerPropagationContract.DefaultMaxAgeSeconds));
    }

    public OwnerScope Current => ResolveCurrentOwner();

    private OwnerScope ResolveCurrentOwner()
    {
        HttpContext? context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return OwnerScope.LocalSingleUser;
        }

        string? authenticatedOwner = ResolveAuthenticatedOwner(context.User);
        if (!string.IsNullOrWhiteSpace(authenticatedOwner))
        {
            return new OwnerScope(authenticatedOwner);
        }

        string? portalOwner = ResolvePortalAuthenticatedOwner(context);
        if (!string.IsNullOrWhiteSpace(portalOwner))
        {
            return new OwnerScope(portalOwner);
        }

        if (!string.IsNullOrWhiteSpace(_headerName))
        {
            string? forwardedOwner = context.Request.Headers[_headerName].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedOwner))
            {
                return new OwnerScope(forwardedOwner);
            }
        }

        return OwnerScope.LocalSingleUser;
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

    private string? ResolvePortalAuthenticatedOwner(HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(_portalOwnerSharedKey))
        {
            return null;
        }

        string? ownerHeader = context.Request.Headers[PortalOwnerPropagationContract.OwnerHeaderName].FirstOrDefault();
        string? timestampHeader = context.Request.Headers[PortalOwnerPropagationContract.TimestampHeaderName].FirstOrDefault();
        string? signatureHeader = context.Request.Headers[PortalOwnerPropagationContract.SignatureHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(ownerHeader)
            || string.IsNullOrWhiteSpace(timestampHeader)
            || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return null;
        }

        string normalizedOwner = new OwnerScope(ownerHeader).NormalizedValue;
        if (string.IsNullOrWhiteSpace(normalizedOwner)
            || !long.TryParse(timestampHeader, NumberStyles.None, CultureInfo.InvariantCulture, out long unixTimestamp))
        {
            return null;
        }

        DateTimeOffset signedAt;
        try
        {
            signedAt = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }

        if ((DateTimeOffset.UtcNow - signedAt).Duration() > _portalOwnerMaxAge)
        {
            return null;
        }

        string expectedSignature = CreatePortalOwnerSignature(normalizedOwner, timestampHeader, _portalOwnerSharedKey);
        return ConstantTimeEquals(expectedSignature, signatureHeader.Trim())
            ? normalizedOwner
            : null;
    }

    private static string CreatePortalOwnerSignature(string normalizedOwner, string unixTimestamp, string sharedKey)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(sharedKey);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(
            PortalOwnerPropagationContract.BuildSignaturePayload(normalizedOwner, unixTimestamp));
        using HMACSHA256 hmac = new(keyBytes);
        return Convert.ToHexString(hmac.ComputeHash(payloadBytes));
    }

    private static bool ConstantTimeEquals(string left, string right)
    {
        byte[] leftBytes = Encoding.UTF8.GetBytes(left);
        byte[] rightBytes = Encoding.UTF8.GetBytes(right);
        if (leftBytes.Length != rightBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
