using System.Linq;
using Chummer.Application.Tools;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;

namespace Chummer.Api.Endpoints;

public static class ShellEndpoints
{
    public static IEndpointRouteBuilder MapShellEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/shell/preferences", (IShellPreferencesService shellPreferencesService) =>
        {
            return Results.Ok(shellPreferencesService.Load());
        });

        app.MapPost("/api/shell/preferences", (ShellUserPreferences? preferences, IShellPreferencesService shellPreferencesService) =>
        {
            shellPreferencesService.Save(preferences ?? ShellUserPreferences.Default);
            return Results.Ok(shellPreferencesService.Load());
        });

        app.MapGet("/api/shell/bootstrap", (string? ruleset, IWorkspaceService workspaceService, IRulesetShellCatalogResolver shellCatalogResolver, IShellPreferencesService shellPreferencesService) =>
        {
            IReadOnlyList<WorkspaceListItem> workspaceList = workspaceService.List(ShellBootstrapDefaults.MaxWorkspaces);
            string preferredRulesetId = RulesetDefaults.Normalize(shellPreferencesService.Load().PreferredRulesetId);
            string activeRulesetId = RulesetDefaults.Normalize(workspaceList.FirstOrDefault()?.RulesetId ?? preferredRulesetId);
            string requestedRulesetId = string.IsNullOrWhiteSpace(ruleset)
                ? RulesetDefaults.Normalize(workspaceList.FirstOrDefault()?.RulesetId ?? preferredRulesetId)
                : RulesetDefaults.Normalize(ruleset);

            IReadOnlyList<WorkspaceListItemResponse> workspaces = workspaceList
                .Select(workspace => new WorkspaceListItemResponse(
                    Id: workspace.Id.Value,
                    Summary: workspace.Summary,
                    LastUpdatedUtc: workspace.LastUpdatedUtc,
                    RulesetId: workspace.RulesetId))
                .ToArray();

            return Results.Ok(new ShellBootstrapResponse(
                RulesetId: requestedRulesetId,
                Commands: shellCatalogResolver.ResolveCommands(requestedRulesetId),
                NavigationTabs: shellCatalogResolver.ResolveNavigationTabs(requestedRulesetId),
                Workspaces: workspaces,
                PreferredRulesetId: preferredRulesetId,
                ActiveRulesetId: activeRulesetId));
        });

        return app;
    }
}
