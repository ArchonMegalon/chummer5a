using Chummer.Application.Workspaces;
using Chummer.Contracts.Api;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Workspaces;

namespace Chummer.Web.Endpoints;

public static class WorkspaceEndpoints
{
    public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/workspaces/import", (IWorkspaceService workspaceService, CharacterXmlRequest request) =>
        {
            WorkspaceImportResult result = workspaceService.Import(request.Xml);
            return Results.Ok(new WorkspaceImportResponse(
                Id: result.Id.Value,
                Summary: result.Summary));
        });

        app.MapGet("/api/workspaces/{id}/profile", (string id, IWorkspaceService workspaceService) =>
        {
            CharacterWorkspaceId workspaceId = new(id);
            var profile = workspaceService.GetProfile(workspaceId);
            return profile is null ? Results.NotFound() : Results.Ok(profile);
        });

        app.MapGet("/api/workspaces/{id}/progress", (string id, IWorkspaceService workspaceService) =>
        {
            CharacterWorkspaceId workspaceId = new(id);
            var progress = workspaceService.GetProgress(workspaceId);
            return progress is null ? Results.NotFound() : Results.Ok(progress);
        });

        app.MapGet("/api/workspaces/{id}/skills", (string id, IWorkspaceService workspaceService) =>
        {
            CharacterWorkspaceId workspaceId = new(id);
            var skills = workspaceService.GetSkills(workspaceId);
            return skills is null ? Results.NotFound() : Results.Ok(skills);
        });

        app.MapMethods("/api/workspaces/{id}/metadata", ["PATCH"], (string id, UpdateWorkspaceMetadata command, IWorkspaceService workspaceService) =>
        {
            CharacterWorkspaceId workspaceId = new(id);
            CommandResult<CharacterProfileSection> result = workspaceService.UpdateMetadata(workspaceId, command);
            if (!result.Success || result.Value is null)
                return Results.NotFound(new { error = result.Error ?? "Workspace not found." });

            return Results.Ok(new WorkspaceMetadataResponse(result.Value));
        });

        app.MapPost("/api/workspaces/{id}/save", (string id, IWorkspaceService workspaceService) =>
        {
            CharacterWorkspaceId workspaceId = new(id);
            CommandResult<string> result = workspaceService.Save(workspaceId);
            if (!result.Success || result.Value is null)
                return Results.NotFound(new { error = result.Error ?? "Workspace not found." });

            return Results.Ok(new WorkspaceSaveResponse(
                Id: workspaceId.Value,
                Xml: result.Value));
        });

        return app;
    }
}
