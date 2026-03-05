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

        app.MapPost("/api/shell/preferences", (ShellPreferences? preferences, IShellPreferencesService shellPreferencesService) =>
        {
            shellPreferencesService.Save(preferences ?? ShellPreferences.Default);
            return Results.Ok(shellPreferencesService.Load());
        });

        app.MapGet("/api/shell/session", (IShellSessionService shellSessionService) =>
        {
            return Results.Ok(shellSessionService.Load());
        });

        app.MapPost("/api/shell/session", (ShellSessionState? session, IShellSessionService shellSessionService) =>
        {
            shellSessionService.Save(session ?? ShellSessionState.Default);
            return Results.Ok(shellSessionService.Load());
        });

        app.MapGet("/api/shell/bootstrap", (string? ruleset, IWorkspaceService workspaceService, IRulesetShellCatalogResolver shellCatalogResolver, IShellPreferencesService shellPreferencesService, IShellSessionService shellSessionService) =>
        {
            IReadOnlyList<WorkspaceListItem> workspaceList = workspaceService.List(ShellBootstrapDefaults.MaxWorkspaces);
            ShellPreferences preferences = shellPreferencesService.Load();
            ShellSessionState session = shellSessionService.Load();
            string preferredRulesetId = RulesetDefaults.Normalize(preferences.PreferredRulesetId);
            CharacterWorkspaceId? activeWorkspaceId = ResolveActiveWorkspaceId(workspaceList, session.ActiveWorkspaceId);
            string activeRulesetId = ResolveRulesetForWorkspace(activeWorkspaceId, workspaceList, preferredRulesetId);
            string requestedRulesetId = string.IsNullOrWhiteSpace(ruleset)
                ? activeRulesetId
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
                ActiveRulesetId: activeRulesetId,
                ActiveWorkspaceId: activeWorkspaceId?.Value,
                ActiveTabId: session.ActiveTabId));
        });

        return app;
    }

    private static CharacterWorkspaceId? ResolveActiveWorkspaceId(
        IReadOnlyList<WorkspaceListItem> workspaces,
        string? preferredActiveWorkspaceId)
    {
        if (!string.IsNullOrWhiteSpace(preferredActiveWorkspaceId))
        {
            WorkspaceListItem? matchingWorkspace = workspaces.FirstOrDefault(workspace =>
                string.Equals(workspace.Id.Value, preferredActiveWorkspaceId, StringComparison.Ordinal));
            if (matchingWorkspace is not null)
            {
                return matchingWorkspace.Id;
            }
        }

        return workspaces.FirstOrDefault()?.Id;
    }

    private static string ResolveRulesetForWorkspace(
        CharacterWorkspaceId? activeWorkspaceId,
        IReadOnlyList<WorkspaceListItem> workspaces,
        string preferredRulesetId)
    {
        if (activeWorkspaceId is null)
        {
            return RulesetDefaults.Normalize(preferredRulesetId);
        }

        WorkspaceListItem? matchingWorkspace = workspaces.FirstOrDefault(workspace =>
            string.Equals(workspace.Id.Value, activeWorkspaceId.Value.Value, StringComparison.Ordinal));
        return matchingWorkspace is null
            ? RulesetDefaults.Normalize(preferredRulesetId)
            : RulesetDefaults.Normalize(matchingWorkspace.RulesetId);
    }
}
