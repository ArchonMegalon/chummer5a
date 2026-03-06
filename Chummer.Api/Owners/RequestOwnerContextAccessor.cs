using System.Security.Claims;
using Chummer.Application.Owners;
using Chummer.Contracts.Owners;

namespace Chummer.Api.Owners;

public sealed class RequestOwnerContextAccessor : IOwnerContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string? _headerName;

    public RequestOwnerContextAccessor(IHttpContextAccessor httpContextAccessor, string? headerName = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _headerName = string.IsNullOrWhiteSpace(headerName)
            ? null
            : headerName.Trim();
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
}
