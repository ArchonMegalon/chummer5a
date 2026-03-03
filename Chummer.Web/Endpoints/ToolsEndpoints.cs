using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Chummer.Application.Tools;
using Chummer.Contracts.Api;

namespace Chummer.Web.Endpoints;

public static class ToolsEndpoints
{
    public static IEndpointRouteBuilder MapToolsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tools/dice/roll", (DiceRollRequest request) =>
        {
            string expression = (request.Expression ?? string.Empty).Trim().ToLowerInvariant();
            Match match = Regex.Match(expression, @"^(?<count>\d{1,3})d(?<sides>\d{1,4})(?<mod>[+-]\d+)?$");
            if (!match.Success)
                return Results.BadRequest(new { error = "Expression must match NdM(+/-X), e.g. 12d6+2." });

            int count = int.Parse(match.Groups["count"].Value);
            int sides = int.Parse(match.Groups["sides"].Value);
            int modifier = match.Groups["mod"].Success ? int.Parse(match.Groups["mod"].Value) : 0;
            if (count < 1 || count > 200 || sides < 2 || sides > 1000)
                return Results.BadRequest(new { error = "Dice count or sides are out of supported range." });

            List<int> rolls = new(count);
            for (int i = 0; i < count; i++)
                rolls.Add(Random.Shared.Next(1, sides + 1));

            int hits = rolls.Count(value => value >= 5);
            int ones = rolls.Count(value => value == 1);
            int rawTotal = rolls.Sum();
            int total = rawTotal + modifier;

            return Results.Ok(new
            {
                expression = $"{count}d{sides}{(modifier > 0 ? "+" : string.Empty)}{(modifier == 0 ? string.Empty : modifier)}",
                rolls,
                rawTotal,
                modifier,
                total,
                hits,
                ones,
                glitch = ones > count / 2,
                criticalGlitch = hits == 0 && ones > count / 2
            });
        });

        app.MapPost("/api/tools/data-export", (IDataExportService dataExportService, CharacterXmlRequest request) =>
        {
            DataExportBundle bundle = dataExportService.BuildBundle(request.Xml);
            return Results.Ok(bundle);
        });

        app.MapGet("/api/tools/master-index", () =>
        {
            string dataDir = Path.Combine(AppContext.BaseDirectory, "data");
            if (!Directory.Exists(dataDir))
                return Results.Ok(new { count = 0, files = Array.Empty<object>() });

            List<object> files = new();
            foreach (string file in Directory.EnumerateFiles(dataDir, "*.xml").OrderBy(Path.GetFileName))
            {
                try
                {
                    XDocument document = XDocument.Load(file, LoadOptions.None);
                    files.Add(new
                    {
                        file = Path.GetFileName(file),
                        root = document.Root?.Name.LocalName ?? string.Empty,
                        elementCount = document.Descendants().Count()
                    });
                }
                catch
                {
                    files.Add(new
                    {
                        file = Path.GetFileName(file),
                        root = string.Empty,
                        elementCount = 0
                    });
                }
            }

            return Results.Ok(new
            {
                count = files.Count,
                generatedUtc = DateTimeOffset.UtcNow,
                files
            });
        });

        app.MapGet("/api/tools/translator/languages", () =>
        {
            string langDir = Path.Combine(AppContext.BaseDirectory, "lang");
            if (!Directory.Exists(langDir))
                return Results.Ok(new { count = 0, languages = Array.Empty<object>() });

            List<object> languages = new();
            foreach (string file in Directory.EnumerateFiles(langDir, "*.xml").OrderBy(Path.GetFileName))
            {
                string code = Path.GetFileNameWithoutExtension(file);
                string name = code;
                try
                {
                    XDocument doc = XDocument.Load(file, LoadOptions.None);
                    name = doc.Root?.Element("name")?.Value?.Trim() ?? code;
                }
                catch
                {
                    name = code;
                }

                languages.Add(new { code, name });
            }

            return Results.Ok(new { count = languages.Count, languages });
        });

        app.MapGet("/api/tools/settings/{scope}", (string scope, ISettingsStore settingsStore) =>
        {
            if (!TryNormalizeScope(scope, out string normalizedScope))
                return Results.BadRequest(new { error = "scope must be 'global' or 'character'." });

            JsonObject settings = settingsStore.Load(normalizedScope);
            return Results.Ok(new { scope = normalizedScope, settings });
        });

        app.MapPost("/api/tools/settings/{scope}", (string scope, JsonObject? settings, ISettingsStore settingsStore) =>
        {
            if (!TryNormalizeScope(scope, out string normalizedScope))
                return Results.BadRequest(new { error = "scope must be 'global' or 'character'." });

            settingsStore.Save(normalizedScope, settings ?? new JsonObject());
            return Results.Ok(new { scope = normalizedScope, saved = true });
        });

        app.MapGet("/api/tools/roster", (IRosterStore rosterStore) =>
        {
            IReadOnlyList<RosterEntry> entries = rosterStore.Load();
            return Results.Ok(new { count = entries.Count, entries });
        });

        app.MapPost("/api/tools/roster", (RosterEntry entry, IRosterStore rosterStore) =>
        {
            IReadOnlyList<RosterEntry> entries = rosterStore.Upsert(entry);
            return Results.Ok(new { count = entries.Count, entries });
        });

        return app;
    }

    private static bool TryNormalizeScope(string scope, out string normalizedScope)
    {
        normalizedScope = (scope ?? string.Empty).Trim().ToLowerInvariant();
        return normalizedScope is "global" or "character";
    }
}
