using System.Text;
using System.Xml;
using System.Linq;
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
            try
            {
                WorkspaceImportResult result = workspaceService.Import(ToImportDocument(request));
                return Results.Ok(new WorkspaceImportResponse(
                    Id: result.Id.Value,
                    Summary: result.Summary));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (FormatException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (XmlException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }

        });

        app.MapGet("/api/workspaces", (IWorkspaceService workspaceService) =>
        {
            IReadOnlyList<WorkspaceListItemResponse> workspaces = workspaceService.List()
                .Select(workspace => new WorkspaceListItemResponse(
                    Id: workspace.Id.Value,
                    Summary: workspace.Summary,
                    LastUpdatedUtc: workspace.LastUpdatedUtc))
                .ToArray();

            return Results.Ok(new WorkspaceListResponse(
                Count: workspaces.Count,
                Workspaces: workspaces));
        });

        app.MapDelete("/api/workspaces/{id}", (string id, IWorkspaceService workspaceService) =>
        {
            CharacterWorkspaceId workspaceId = new(id);
            bool deleted = workspaceService.Close(workspaceId);
            return deleted ? Results.NoContent() : Results.NotFound(new { error = "Workspace not found." });
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

        app.MapGet("/api/workspaces/{id}/summary", (string id, IWorkspaceService workspaceService) =>
        {
            CharacterWorkspaceId workspaceId = new(id);
            CharacterFileSummary? summary = workspaceService.GetSummary(workspaceId);
            return summary is null ? Results.NotFound() : Results.Ok(summary);
        });

        app.MapGet("/api/workspaces/{id}/validate", (string id, IWorkspaceService workspaceService) =>
        {
            CharacterWorkspaceId workspaceId = new(id);
            CharacterValidationResult? validation = workspaceService.Validate(workspaceId);
            return validation is null ? Results.NotFound() : Results.Ok(validation);
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

    private static WorkspaceImportDocument ToImportDocument(WorkspaceImportRequest request)
    {
        WorkspaceDocumentFormat format = ParseFormatOrDefault(request.Format);

        if (!string.IsNullOrWhiteSpace(request.ContentBase64))
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(request.ContentBase64);
                string content = Encoding.UTF8.GetString(bytes);
                return new WorkspaceImportDocument(content, format);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Import payload contentBase64 is not valid base64.", ex);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Xml))
            return new WorkspaceImportDocument(request.Xml, format);

        throw new InvalidOperationException("Workspace import requires either contentBase64 or xml.");
    }

    private static WorkspaceDocumentFormat ParseFormatOrDefault(string? rawFormat)
    {
        if (Enum.TryParse(rawFormat, ignoreCase: true, out WorkspaceDocumentFormat format))
            return format;

        return WorkspaceDocumentFormat.Chum5Xml;
    }
}
