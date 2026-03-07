using Microsoft.AspNetCore.Http;

public static class PortalProtectedRouteMatcher
{
    public static bool RequiresAuthenticatedUser(PathString path)
    {
        return path.StartsWithSegments("/api", StringComparison.Ordinal)
            || path.StartsWithSegments("/openapi", StringComparison.Ordinal)
            || path.StartsWithSegments("/docs", StringComparison.Ordinal)
            || path.StartsWithSegments("/blazor", StringComparison.Ordinal)
            || path.StartsWithSegments("/hub", StringComparison.Ordinal)
            || path.StartsWithSegments("/avalonia", StringComparison.Ordinal);
    }
}
