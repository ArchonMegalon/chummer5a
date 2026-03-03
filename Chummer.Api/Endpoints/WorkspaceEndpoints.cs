using Chummer.Application.Workspaces;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Workspaces;

namespace Chummer.Api.Endpoints;

public static class WorkspaceEndpoints
{
    public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/workspaces/import", (IWorkspaceService workspaceService, WorkspaceImportRequest request) =>
        {
            WorkspaceImportResult result = workspaceService.Import(new WorkspaceImportDocument(request.Xml));
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

        app.MapGet("/api/workspaces/{id}/rules", (string id, IWorkspaceService workspaceService) =>
        {
            CharacterWorkspaceId workspaceId = new(id);
            var rules = workspaceService.GetRules(workspaceId);
            return rules is null ? Results.NotFound() : Results.Ok(rules);
        });

        app.MapGet("/api/workspaces/{id}/build", (string id, IWorkspaceService workspaceService) =>
        {
            CharacterWorkspaceId workspaceId = new(id);
            var build = workspaceService.GetBuild(workspaceId);
            return build is null ? Results.NotFound() : Results.Ok(build);
        });

        app.MapGet("/api/workspaces/{id}/movement", (string id, IWorkspaceService workspaceService) =>
        {
            CharacterWorkspaceId workspaceId = new(id);
            var movement = workspaceService.GetMovement(workspaceId);
            return movement is null ? Results.NotFound() : Results.Ok(movement);
        });

        app.MapGet("/api/workspaces/{id}/awakening", (string id, IWorkspaceService workspaceService) =>
        {
            CharacterWorkspaceId workspaceId = new(id);
            var awakening = workspaceService.GetAwakening(workspaceId);
            return awakening is null ? Results.NotFound() : Results.Ok(awakening);
        });

        app.MapGet("/api/workspaces/{id}/sections/{sectionId}", (string id, string sectionId, IWorkspaceService workspaceService) =>
        {
            CharacterWorkspaceId workspaceId = new(id);
            try
            {
                object? section = workspaceService.GetSection(workspaceId, sectionId);
                return section is null ? Results.NotFound() : Results.Ok(section);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
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
            CommandResult<WorkspaceSaveReceipt> result = workspaceService.Save(workspaceId);
            if (!result.Success || result.Value is null)
                return Results.NotFound(new { error = result.Error ?? "Workspace not found." });

            return Results.Ok(new WorkspaceSaveResponse(
                Id: result.Value.Id.Value,
                DocumentLength: result.Value.DocumentLength));
        });

        return app;
    }
}
