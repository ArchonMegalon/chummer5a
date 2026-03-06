using System.Security.Claims;
using Chummer.Contracts.Owners;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Routing;

public static class PortalAuthenticationEndpoints
{
    public static void MapPortalAuthenticationEndpoints(IEndpointRouteBuilder endpoints, PortalAuthenticationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(settings);

        endpoints.MapGet("/auth/me", (HttpContext context) =>
            Results.Ok(BuildSummary(context.User, settings.DevAuthEnabled, settings.RequireAuthenticatedUser)));

        endpoints.MapPost("/auth/dev-login", async (HttpContext context, PortalDevLoginRequest? request) =>
        {
            if (!settings.DevAuthEnabled)
            {
                return Results.NotFound(new
                {
                    error = "portal_dev_auth_disabled"
                });
            }

            ClaimsPrincipal principal = CreatePrincipal(request?.Owner, settings.DefaultDevUser);
            await context.SignInAsync(settings.CookieScheme, principal);
            return Results.Ok(BuildSummary(principal, settings.DevAuthEnabled, settings.RequireAuthenticatedUser));
        });

        endpoints.MapPost("/auth/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(settings.CookieScheme);
            return Results.Ok(new
            {
                ok = true
            });
        });
    }

    public static ClaimsPrincipal CreatePrincipal(string? requestedOwner, string defaultOwner)
    {
        string owner = new OwnerScope(string.IsNullOrWhiteSpace(requestedOwner) ? defaultOwner : requestedOwner).NormalizedValue;
        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, owner),
            new Claim(ClaimTypes.Name, owner)
        ];

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "portal-dev"));
    }

    public static PortalAuthSummary BuildSummary(
        ClaimsPrincipal? principal,
        bool devAuthEnabled,
        bool requireAuthenticatedUser)
    {
        string? owner = principal?.Identity?.IsAuthenticated == true
            ? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst("sub")?.Value
                ?? principal.Identity?.Name
            : null;

        return new PortalAuthSummary(
            IsAuthenticated: principal?.Identity?.IsAuthenticated == true,
            Owner: new OwnerScope(owner ?? string.Empty).NormalizedValue,
            AuthenticationType: principal?.Identity?.AuthenticationType ?? string.Empty,
            DevAuthEnabled: devAuthEnabled,
            RequireAuthenticatedUser: requireAuthenticatedUser);
    }
}

public sealed record PortalDevLoginRequest(string? Owner);

public sealed record PortalAuthSummary(
    bool IsAuthenticated,
    string Owner,
    string AuthenticationType,
    bool DevAuthEnabled,
    bool RequireAuthenticatedUser);
