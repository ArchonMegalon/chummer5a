using System.Text.RegularExpressions;
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

        app.MapGet("/api/tools/master-index", (IToolCatalogService toolCatalogService) =>
        {
            MasterIndexResponse response = toolCatalogService.GetMasterIndex();
            return Results.Ok(response);
        });

        app.MapGet("/api/tools/translator/languages", (IToolCatalogService toolCatalogService) =>
        {
            TranslatorLanguagesResponse response = toolCatalogService.GetTranslatorLanguages();
            return Results.Ok(response);
        });

        return app;
    }
}
