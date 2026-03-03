using System.Xml;

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
                "/api/workspaces/import"
            }
        }));

        app.MapGet("/api/info", () => Results.Ok(new
        {
            service = "Chummer",
            status = "running",
            runtime = "net10.0",
            platform = "linux-native"
        }));

        app.MapGet("/api/health", () => Results.Ok(new { ok = true, utc = DateTimeOffset.UtcNow }));

        app.MapPost("/api/xml/is-empty", (string xml) =>
        {
            XmlDocument doc = new();
            doc.LoadXml(xml);
            bool isEmpty = doc.DocumentElement is null || string.IsNullOrWhiteSpace(doc.DocumentElement.InnerText);
            return Results.Ok(new { isEmpty });
        });

        return app;
    }
}
