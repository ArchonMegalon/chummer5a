using Microsoft.AspNetCore.Authentication.Cookies;

public sealed record PortalAuthenticationSettings(
    bool DevAuthEnabled,
    bool RequireAuthenticatedUser,
    string DefaultDevUser,
    string CookieScheme = CookieAuthenticationDefaults.AuthenticationScheme);
