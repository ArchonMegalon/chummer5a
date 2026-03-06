using Chummer.Application.Content;
using System.Linq;
using Chummer.Application.Owners;
using Chummer.Application.Tools;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;

namespace Chummer.Api.Endpoints;

public static class ShellEndpoints
{
    public static IEndpointRouteBuilder MapShellEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/shell/preferences", (IShellPreferencesService shellPreferencesService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            return Results.Ok(shellPreferencesService.Load(owner));
        });

        app.MapPost("/api/shell/preferences", (ShellPreferences? preferences, IShellPreferencesService shellPreferencesService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            shellPreferencesService.Save(owner, preferences ?? ShellPreferences.Default);
            return Results.Ok(shellPreferencesService.Load(owner));
        });

        app.MapGet("/api/shell/session", (IShellSessionService shellSessionService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            return Results.Ok(shellSessionService.Load(owner));
        });

        app.MapPost("/api/shell/session", (ShellSessionState? session, IShellSessionService shellSessionService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            shellSessionService.Save(owner, session ?? ShellSessionState.Default);
            return Results.Ok(shellSessionService.Load(owner));
        });

        app.MapGet("/api/shell/bootstrap", (string? ruleset, IWorkspaceService workspaceService, IRulesetShellCatalogResolver shellCatalogResolver, IRulesetSelectionPolicy rulesetSelectionPolicy, IShellPreferencesService shellPreferencesService, IShellSessionService shellSessionService, IActiveRuntimeStatusService activeRuntimeStatusService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            IReadOnlyList<WorkspaceListItem> workspaceList = workspaceService.List(owner, ShellBootstrapDefaults.MaxWorkspaces);
            ShellPreferences preferences = shellPreferencesService.Load(owner);
            ShellSessionState session = shellSessionService.Load(owner);
            string fallbackRulesetId = rulesetSelectionPolicy.GetDefaultRulesetId();
            string preferredRulesetId = ResolvePreferredRulesetId(preferences.PreferredRulesetId, workspaceList, fallbackRulesetId);
            CharacterWorkspaceId? activeWorkspaceId = ResolveActiveWorkspaceId(workspaceList, session.ActiveWorkspaceId);
            string activeRulesetId = ResolveRulesetForWorkspace(activeWorkspaceId, workspaceList, preferredRulesetId, fallbackRulesetId);
            string requestedRulesetId = RulesetDefaults.NormalizeOptional(ruleset)
                ?? activeRulesetId
                ?? fallbackRulesetId;
            string effectivePreferredRulesetId = string.IsNullOrWhiteSpace(preferredRulesetId)
                ? requestedRulesetId
                : preferredRulesetId;
            string effectiveActiveRulesetId = string.IsNullOrWhiteSpace(activeRulesetId)
                ? requestedRulesetId
                : activeRulesetId;

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
                PreferredRulesetId: effectivePreferredRulesetId,
                ActiveRulesetId: effectiveActiveRulesetId,
                ActiveWorkspaceId: activeWorkspaceId?.Value,
                ActiveTabId: session.ActiveTabId,
                ActiveTabsByWorkspace: session.ActiveTabsByWorkspace,
                WorkflowDefinitions: shellCatalogResolver.ResolveWorkflowDefinitions(requestedRulesetId),
                WorkflowSurfaces: shellCatalogResolver.ResolveWorkflowSurfaces(requestedRulesetId),
                ActiveRuntime: activeRuntimeStatusService.GetActiveProfileStatus(owner, requestedRulesetId)));
        }).AllowPublicApiKeyBypass();

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

    private static string ResolvePreferredRulesetId(
        string? preferredRulesetId,
        IReadOnlyList<WorkspaceListItem> workspaces,
        string fallbackRulesetId)
    {
        return RulesetDefaults.NormalizeOptional(preferredRulesetId)
            ?? workspaces
                .Select(workspace => RulesetDefaults.NormalizeOptional(workspace.RulesetId))
                .FirstOrDefault(rulesetId => rulesetId is not null)
            ?? fallbackRulesetId;
    }

    private static string ResolveRulesetForWorkspace(
        CharacterWorkspaceId? activeWorkspaceId,
        IReadOnlyList<WorkspaceListItem> workspaces,
        string preferredRulesetId,
        string fallbackRulesetId)
    {
        if (activeWorkspaceId is null)
        {
            return RulesetDefaults.NormalizeOptional(preferredRulesetId)
                ?? fallbackRulesetId;
        }

        WorkspaceListItem? matchingWorkspace = workspaces.FirstOrDefault(workspace =>
            string.Equals(workspace.Id.Value, activeWorkspaceId.Value.Value, StringComparison.Ordinal));
        return matchingWorkspace is null
            ? RulesetDefaults.NormalizeOptional(preferredRulesetId) ?? fallbackRulesetId
            : RulesetDefaults.NormalizeOptional(matchingWorkspace.RulesetId)
                ?? RulesetDefaults.NormalizeOptional(preferredRulesetId)
                ?? fallbackRulesetId;
    }
}
