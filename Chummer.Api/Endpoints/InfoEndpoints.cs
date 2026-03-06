using System.Xml;
using Chummer.Application.Content;

namespace Chummer.Api.Endpoints;

public static class InfoEndpoints
{
    public static IEndpointRouteBuilder MapInfoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Ok(new
        {
            service = "Chummer.Api",
            status = "running",
            docs = new[]
            {
                "/api/info",
                "/api/health",
                "/api/hub/search",
                "/api/hub/projects/{kind}/{itemId}",
                "/api/hub/projects/{kind}/{itemId}/install-preview",
                "/api/hub/projects/{kind}/{itemId}/compatibility",
                "/api/hub/publish/drafts",
                "/api/hub/publish/drafts/{draftId}",
                "/api/hub/publish/drafts/{draftId}/archive",
                "/api/hub/publish/{kind}/{itemId}/submit",
                "/api/hub/moderation/queue",
                "/api/content/overlays",
                "/api/buildkits",
                "/api/rulepacks",
                "/api/rulepacks/{packId}/install-preview",
                "/api/rulepacks/{packId}/install",
                "/api/profiles",
                "/api/runtime/profiles/{profileId}",
                "/api/runtime/locks",
                "/api/runtime/locks/{lockId}",
                "/api/runtime/locks/{lockId}/install-preview",
                "/api/runtime/locks/{lockId}/install",
                "/api/workspaces",
                "/api/workspaces/import"
            }
        }));

        app.MapGet("/api/info", (IContentOverlayCatalogService overlays) => Results.Ok(new
        {
            service = "Chummer",
            status = "running",
            runtime = "net10.0",
            platform = "linux-native",
            content = ToOverlayResponse(overlays.GetCatalog())
        })).AllowPublicApiKeyBypass();

        app.MapGet("/api/content/overlays", (IContentOverlayCatalogService overlays) =>
            Results.Ok(ToOverlayResponse(overlays.GetCatalog()))).AllowPublicApiKeyBypass();

        app.MapGet("/api/health", () => Results.Ok(new { ok = true, utc = DateTimeOffset.UtcNow }))
            .AllowPublicApiKeyBypass();

        app.MapPost("/api/xml/is-empty", (string xml) =>
        {
            XmlDocument doc = new();
            doc.LoadXml(xml);
            bool isEmpty = doc.DocumentElement is null || string.IsNullOrWhiteSpace(doc.DocumentElement.InnerText);
            return Results.Ok(new { isEmpty });
        });

        return app;
    }

    private static object ToOverlayResponse(ContentOverlayCatalog catalog)
    {
        return new
        {
            baseDataPath = catalog.BaseDataPath,
            baseLanguagePath = catalog.BaseLanguagePath,
            overlays = catalog.Overlays.Select(overlay => new
            {
                id = overlay.Id,
                name = overlay.Name,
                rootPath = overlay.RootPath,
                dataPath = overlay.DataPath,
                languagePath = overlay.LanguagePath,
                priority = overlay.Priority,
                enabled = overlay.Enabled,
                mode = overlay.Mode,
                description = overlay.Description
            })
        };
    }
}
