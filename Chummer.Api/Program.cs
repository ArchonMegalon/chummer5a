using System.Security.Cryptography;
using System.Text;
using Chummer.Infrastructure.DependencyInjection;
using Chummer.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddChummerHeadlessCore(AppContext.BaseDirectory, Directory.GetCurrentDirectory());
builder.Services.AddOpenApi();

var app = builder.Build();

string? configuredApiKey = builder.Configuration["Chummer:ApiKey"];
string? environmentApiKey = Environment.GetEnvironmentVariable("CHUMMER_API_KEY");
string? apiKey = configuredApiKey ?? environmentApiKey;

if (!string.IsNullOrWhiteSpace(apiKey))
{
    app.Use(async (context, next) =>
    {
        PathString path = context.Request.Path;
        if (!path.StartsWithSegments("/api", StringComparison.Ordinal))
        {
            await next();
            return;
        }

        if (IsPublicApiPath(path) || HttpMethods.IsOptions(context.Request.Method))
        {
            await next();
            return;
        }

        string? provided = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(provided) || !ConstantTimeEquals(provided, apiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "missing_or_invalid_api_key",
                header = "X-Api-Key"
            });
            return;
        }

        await next();
    });
}
else if (app.Environment.IsProduction())
{
    Console.WriteLine("WARNING: CHUMMER_API_KEY is not configured; API key middleware is disabled.");
}

app.MapOpenApi("/openapi/{documentName}.json");
app.Map("/docs", docsApp =>
{
    docsApp.Run(async context =>
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(GetDocsHtml());
    });
});

app.MapInfoEndpoints();
app.MapCharacterEndpoints();
app.MapLifeModulesEndpoints();
app.MapToolsEndpoints();
app.MapSettingsEndpoints();
app.MapRosterEndpoints();
app.MapCommandEndpoints();
app.MapNavigationEndpoints();
app.MapWorkspaceEndpoints();

app.Run();

static bool IsPublicApiPath(PathString path)
{
    return path.StartsWithSegments("/api/health", StringComparison.Ordinal)
        || path.StartsWithSegments("/api/info", StringComparison.Ordinal)
        || path.StartsWithSegments("/api/commands", StringComparison.Ordinal)
        || path.StartsWithSegments("/api/navigation-tabs", StringComparison.Ordinal);
}

static bool ConstantTimeEquals(string left, string right)
{
    byte[] leftBytes = Encoding.UTF8.GetBytes(left);
    byte[] rightBytes = Encoding.UTF8.GetBytes(right);
    if (leftBytes.Length != rightBytes.Length)
        return false;

    return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
}

static string GetDocsHtml()
{
    return """
<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Chummer API Docs</title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/swagger-ui-dist@5/swagger-ui.css" />
    <style>
      body { margin: 0; background: #f7f8fc; font-family: "Segoe UI", system-ui, sans-serif; }
      header { padding: 16px 20px; background: #101622; color: #f4f8ff; }
      header h1 { margin: 0; font-size: 20px; }
      header p { margin: 4px 0 0; opacity: 0.85; font-size: 13px; }
      #swagger-ui { padding: 12px 12px 24px; }
    </style>
  </head>
  <body>
    <header>
      <h1>Chummer API</h1>
      <p>OpenAPI v1 interactive reference</p>
    </header>
    <div id="swagger-ui"></div>
    <script src="https://cdn.jsdelivr.net/npm/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
    <script>
      SwaggerUIBundle({
        url: '/openapi/v1.json',
        dom_id: '#swagger-ui',
        deepLinking: true,
        displayRequestDuration: true,
        docExpansion: 'list'
      });
    </script>
  </body>
</html>
""";
}
