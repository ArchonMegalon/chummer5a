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
            string preferredRulesetId = RulesetDefaults.NormalizeOrDefault(
                preferences.PreferredRulesetId,
                ShellPreferences.Default.PreferredRulesetId);
            CharacterWorkspaceId? activeWorkspaceId = ResolveActiveWorkspaceId(workspaceList, session.ActiveWorkspaceId);
            string activeRulesetId = ResolveRulesetForWorkspace(activeWorkspaceId, workspaceList, preferredRulesetId);
            string requestedRulesetId = RulesetDefaults.NormalizeOptional(ruleset) ?? activeRulesetId;

            IReadOnlyList<WorkspaceListItemResponse> workspaces = workspaceList
                .Select(workspace => new WorkspaceListItemResponse(
                    Id: workspace.Id.Value,
                    Summary: workspace.Summary,
                    LastUpdatedUtc: workspace.LastUpdatedUtc,
                    RulesetId: workspace.RulesetId,
                    HasSavedWorkspace: workspace.HasSavedWorkspace))
                .ToArray();

            return Results.Ok(new ShellBootstrapResponse(
                RulesetId: requestedRulesetId,
                Commands: shellCatalogResolver.ResolveCommands(requestedRulesetId),
                NavigationTabs: shellCatalogResolver.ResolveNavigationTabs(requestedRulesetId),
                Workspaces: workspaces,
                PreferredRulesetId: preferredRulesetId,
                ActiveRulesetId: activeRulesetId,
                ActiveWorkspaceId: activeWorkspaceId?.Value,
                ActiveTabId: session.ActiveTabId,
                ActiveTabsByWorkspace: session.ActiveTabsByWorkspace));
        });

        return app;
    }

    private static CharacterWorkspaceId? ResolveActiveWorkspaceId(
        IReadOnlyList<WorkspaceListItem> workspaces,
        string? preferredActiveWorkspaceId)
    {
        if (string.IsNullOrWhiteSpace(preferredActiveWorkspaceId))
            return null;

        WorkspaceListItem? matchingWorkspace = workspaces.FirstOrDefault(workspace =>
            string.Equals(workspace.Id.Value, preferredActiveWorkspaceId, StringComparison.Ordinal));
        return matchingWorkspace?.Id;
    }

    private static string ResolveRulesetForWorkspace(
        CharacterWorkspaceId? activeWorkspaceId,
        IReadOnlyList<WorkspaceListItem> workspaces,
        string preferredRulesetId)
    {
        if (activeWorkspaceId is null)
        {
            return RulesetDefaults.NormalizeOrDefault(preferredRulesetId, ShellPreferences.Default.PreferredRulesetId);
        }

        WorkspaceListItem? matchingWorkspace = workspaces.FirstOrDefault(workspace =>
            string.Equals(workspace.Id.Value, activeWorkspaceId.Value.Value, StringComparison.Ordinal));
        return matchingWorkspace is null
            ? RulesetDefaults.NormalizeOrDefault(preferredRulesetId, ShellPreferences.Default.PreferredRulesetId)
            : RulesetDefaults.NormalizeOrDefault(matchingWorkspace.RulesetId, preferredRulesetId);
    }
}
