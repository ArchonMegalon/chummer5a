using System.Text;
using System.Xml;
using System.Linq;
using Chummer.Application.Owners;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Api;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;

namespace Chummer.Api.Endpoints;

public static class WorkspaceEndpoints
{
    private const int DefaultWorkspaceListCount = 100;
    private const int MaxWorkspaceListCount = 500;

    public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/workspaces/import", (IWorkspaceService workspaceService, WorkspaceImportRequest request, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            try
            {
                WorkspaceImportResult result = workspaceService.Import(owner, ToImportDocument(request));
                return Results.Ok(new WorkspaceImportResponse(
                    Id: result.Id.Value,
                    Summary: result.Summary,
                    RulesetId: result.RulesetId));
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

        app.MapGet("/api/workspaces", (IWorkspaceService workspaceService, int? maxCount, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            int effectiveMaxCount = ResolveWorkspaceListCount(maxCount);
            IReadOnlyList<WorkspaceListItemResponse> workspaces = workspaceService.List(owner, effectiveMaxCount)
                .Select(workspace => new WorkspaceListItemResponse(
                    Id: workspace.Id.Value,
                    Summary: workspace.Summary,
                    LastUpdatedUtc: workspace.LastUpdatedUtc,
                    RulesetId: workspace.RulesetId,
                    HasSavedWorkspace: workspace.HasSavedWorkspace))
                .ToArray();

            return Results.Ok(new WorkspaceListResponse(
                Count: workspaces.Count,
                Workspaces: workspaces));
        });

        app.MapDelete("/api/workspaces/{id}", (string id, IWorkspaceService workspaceService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            CharacterWorkspaceId workspaceId = new(id);
            bool deleted = workspaceService.Close(owner, workspaceId);
            return deleted ? Results.NoContent() : Results.NotFound(new { error = "Workspace not found." });
        });

        app.MapGet("/api/workspaces/{id}/profile", (string id, IWorkspaceService workspaceService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            CharacterWorkspaceId workspaceId = new(id);
            var profile = workspaceService.GetProfile(owner, workspaceId);
            return profile is null ? Results.NotFound() : Results.Ok(profile);
        });

        app.MapGet("/api/workspaces/{id}/progress", (string id, IWorkspaceService workspaceService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            CharacterWorkspaceId workspaceId = new(id);
            var progress = workspaceService.GetProgress(owner, workspaceId);
            return progress is null ? Results.NotFound() : Results.Ok(progress);
        });

        app.MapGet("/api/workspaces/{id}/skills", (string id, IWorkspaceService workspaceService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            CharacterWorkspaceId workspaceId = new(id);
            var skills = workspaceService.GetSkills(owner, workspaceId);
            return skills is null ? Results.NotFound() : Results.Ok(skills);
        });

        app.MapGet("/api/workspaces/{id}/rules", (string id, IWorkspaceService workspaceService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            CharacterWorkspaceId workspaceId = new(id);
            var rules = workspaceService.GetRules(owner, workspaceId);
            return rules is null ? Results.NotFound() : Results.Ok(rules);
        });

        app.MapGet("/api/workspaces/{id}/build", (string id, IWorkspaceService workspaceService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            CharacterWorkspaceId workspaceId = new(id);
            var build = workspaceService.GetBuild(owner, workspaceId);
            return build is null ? Results.NotFound() : Results.Ok(build);
        });

        app.MapGet("/api/workspaces/{id}/movement", (string id, IWorkspaceService workspaceService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            CharacterWorkspaceId workspaceId = new(id);
            var movement = workspaceService.GetMovement(owner, workspaceId);
            return movement is null ? Results.NotFound() : Results.Ok(movement);
        });

        app.MapGet("/api/workspaces/{id}/awakening", (string id, IWorkspaceService workspaceService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            CharacterWorkspaceId workspaceId = new(id);
            var awakening = workspaceService.GetAwakening(owner, workspaceId);
            return awakening is null ? Results.NotFound() : Results.Ok(awakening);
        });

        app.MapGet("/api/workspaces/{id}/sections/{sectionId}", (string id, string sectionId, IWorkspaceService workspaceService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            CharacterWorkspaceId workspaceId = new(id);
            try
            {
                object? section = workspaceService.GetSection(owner, workspaceId, sectionId);
                return section is null ? Results.NotFound() : Results.Ok(section);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapGet("/api/workspaces/{id}/summary", (string id, IWorkspaceService workspaceService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            CharacterWorkspaceId workspaceId = new(id);
            CharacterFileSummary? summary = workspaceService.GetSummary(owner, workspaceId);
            return summary is null ? Results.NotFound() : Results.Ok(summary);
        });

        app.MapGet("/api/workspaces/{id}/validate", (string id, IWorkspaceService workspaceService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            CharacterWorkspaceId workspaceId = new(id);
            CharacterValidationResult? validation = workspaceService.Validate(owner, workspaceId);
            return validation is null ? Results.NotFound() : Results.Ok(validation);
        });

        app.MapMethods("/api/workspaces/{id}/metadata", ["PATCH"], (string id, UpdateWorkspaceMetadata command, IWorkspaceService workspaceService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            CharacterWorkspaceId workspaceId = new(id);
            CommandResult<CharacterProfileSection> result = workspaceService.UpdateMetadata(owner, workspaceId, command);
            if (!result.Success || result.Value is null)
                return Results.NotFound(new { error = result.Error ?? "Workspace not found." });

            return Results.Ok(new WorkspaceMetadataResponse(result.Value));
        });

        app.MapPost("/api/workspaces/{id}/save", (string id, IWorkspaceService workspaceService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            CharacterWorkspaceId workspaceId = new(id);
            CommandResult<WorkspaceSaveReceipt> result = workspaceService.Save(owner, workspaceId);
            if (!result.Success || result.Value is null)
                return Results.NotFound(new { error = result.Error ?? "Workspace not found." });

            return Results.Ok(new WorkspaceSaveResponse(
                Id: result.Value.Id.Value,
                DocumentLength: result.Value.DocumentLength,
                RulesetId: result.Value.RulesetId));
        });

        app.MapPost("/api/workspaces/{id}/download", (string id, IWorkspaceService workspaceService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            CharacterWorkspaceId workspaceId = new(id);
            CommandResult<WorkspaceDownloadReceipt> result = workspaceService.Download(owner, workspaceId);
            if (!result.Success || result.Value is null)
                return Results.NotFound(new { error = result.Error ?? "Workspace not found." });

            return Results.Ok(new WorkspaceDownloadResponse(
                Id: result.Value.Id.Value,
                Format: result.Value.Format.ToString(),
                ContentBase64: result.Value.ContentBase64,
                FileName: result.Value.FileName,
                DocumentLength: result.Value.DocumentLength,
                RulesetId: result.Value.RulesetId));
        });

        app.MapGet("/api/workspaces/{id}/export", (string id, IWorkspaceService workspaceService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            CharacterWorkspaceId workspaceId = new(id);
            CommandResult<WorkspaceExportReceipt> result = workspaceService.Export(owner, workspaceId);
            if (!result.Success || result.Value is null)
                return Results.NotFound(new { error = result.Error ?? "Workspace not found." });

            return Results.Ok(new WorkspaceExportResponse(
                Id: result.Value.Id.Value,
                Format: result.Value.Format.ToString(),
                ContentBase64: result.Value.ContentBase64,
                FileName: result.Value.FileName,
                DocumentLength: result.Value.DocumentLength,
                RulesetId: result.Value.RulesetId));
        });

        app.MapGet("/api/workspaces/{id}/print", (string id, IWorkspaceService workspaceService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            OwnerScope owner = ownerContextAccessor.Current;
            CharacterWorkspaceId workspaceId = new(id);
            CommandResult<WorkspacePrintReceipt> result = workspaceService.Print(owner, workspaceId);
            if (!result.Success || result.Value is null)
                return Results.NotFound(new { error = result.Error ?? "Workspace not found." });

            return Results.Ok(new WorkspacePrintResponse(
                Id: result.Value.Id.Value,
                ContentBase64: result.Value.ContentBase64,
                FileName: result.Value.FileName,
                MimeType: result.Value.MimeType,
                DocumentLength: result.Value.DocumentLength,
                Title: result.Value.Title,
                RulesetId: result.Value.RulesetId));
        });

        return app;
    }

    private static WorkspaceImportDocument ToImportDocument(WorkspaceImportRequest request)
    {
        WorkspaceDocumentFormat format = ParseFormatOrDefault(request.Format);
        string? rulesetId = RulesetDefaults.NormalizeOptional(request.RulesetId);
        if (rulesetId is null)
            throw new InvalidOperationException("Workspace import rulesetId is required.");

        if (!string.IsNullOrWhiteSpace(request.ContentBase64))
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(request.ContentBase64);
                string content = Encoding.UTF8.GetString(bytes);
                return new WorkspaceImportDocument(content, rulesetId, format);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Import payload contentBase64 is not valid base64.", ex);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Xml))
            return new WorkspaceImportDocument(request.Xml, rulesetId, format);

        throw new InvalidOperationException("Workspace import requires either contentBase64 or xml.");
    }

    private static WorkspaceDocumentFormat ParseFormatOrDefault(string? rawFormat)
    {
        if (Enum.TryParse(rawFormat, ignoreCase: true, out WorkspaceDocumentFormat format))
            return format;

        return WorkspaceDocumentFormat.NativeXml;
    }

    private static int ResolveWorkspaceListCount(int? requestedMaxCount)
    {
        if (requestedMaxCount is > 0)
            return Math.Clamp(requestedMaxCount.Value, 1, MaxWorkspaceListCount);

        return DefaultWorkspaceListCount;
    }
}
