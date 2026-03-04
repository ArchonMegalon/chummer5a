using Chummer.Application.Characters;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Workspaces;
using System.Text;

namespace Chummer.Infrastructure.Workspaces;

public sealed class WorkspaceService : IWorkspaceService
{
    private readonly IWorkspaceStore _workspaceStore;
    private readonly ICharacterFileQueries _characterFileQueries;
    private readonly ICharacterSectionQueries _characterSectionQueries;
    private readonly ICharacterMetadataCommands _characterMetadataCommands;

    public WorkspaceService(
        IWorkspaceStore workspaceStore,
        ICharacterFileQueries characterFileQueries,
        ICharacterSectionQueries characterSectionQueries,
        ICharacterMetadataCommands characterMetadataCommands)
    {
        _workspaceStore = workspaceStore;
        _characterFileQueries = characterFileQueries;
        _characterSectionQueries = characterSectionQueries;
        _characterMetadataCommands = characterMetadataCommands;
    }

    public WorkspaceImportResult Import(WorkspaceImportDocument document)
    {
        string xml = ToXmlContent(document.Content, document.Format);
        CharacterFileSummary summary = _characterFileQueries.ParseSummary(new CharacterDocument(xml));
        CharacterWorkspaceId id = _workspaceStore.Create(new WorkspaceDocument(xml, document.Format));
        return new WorkspaceImportResult(id, summary);
    }

    public IReadOnlyList<WorkspaceListItem> List()
    {
        List<WorkspaceListItem> workspaces = [];

        foreach (WorkspaceStoreEntry entry in _workspaceStore.List())
        {
            CharacterWorkspaceId id = entry.Id;
            if (!_workspaceStore.TryGet(id, out WorkspaceDocument document))
                continue;

            CharacterFileSummary summary;
            try
            {
                summary = _characterFileQueries.ParseSummary(new CharacterDocument(ToXmlContent(document.Content, document.Format)));
            }
            catch
            {
                summary = new CharacterFileSummary(
                    Name: $"Workspace {id.Value}",
                    Alias: string.Empty,
                    Metatype: string.Empty,
                    BuildMethod: string.Empty,
                    CreatedVersion: string.Empty,
                    AppVersion: string.Empty,
                    Karma: 0m,
                    Nuyen: 0m,
                    Created: false);
            }

            workspaces.Add(new WorkspaceListItem(
                Id: id,
                Summary: summary,
                LastUpdatedUtc: entry.LastUpdatedUtc));
        }

        return workspaces;
    }

    public bool Close(CharacterWorkspaceId id)
    {
        return _workspaceStore.Delete(id);
    }

    public object? GetSection(CharacterWorkspaceId id, string sectionId)
    {
        if (!_workspaceStore.TryGet(id, out WorkspaceDocument document))
            return null;

        string xml = ToXmlContent(document.Content, document.Format);
        return _characterSectionQueries.ParseSection(sectionId, new CharacterDocument(xml));
    }

    public CharacterFileSummary? GetSummary(CharacterWorkspaceId id)
    {
        if (!_workspaceStore.TryGet(id, out WorkspaceDocument document))
            return null;

        string xml = ToXmlContent(document.Content, document.Format);
        return _characterFileQueries.ParseSummary(new CharacterDocument(xml));
    }

    public CharacterValidationResult? Validate(CharacterWorkspaceId id)
    {
        if (!_workspaceStore.TryGet(id, out WorkspaceDocument document))
            return null;

        string xml = ToXmlContent(document.Content, document.Format);
        return _characterFileQueries.Validate(new CharacterDocument(xml));
    }

    public CharacterProfileSection? GetProfile(CharacterWorkspaceId id)
    {
        return TryParseSection<CharacterProfileSection>(id, "profile");
    }

    public CharacterProgressSection? GetProgress(CharacterWorkspaceId id)
    {
        return TryParseSection<CharacterProgressSection>(id, "progress");
    }

    public CharacterSkillsSection? GetSkills(CharacterWorkspaceId id)
    {
        return TryParseSection<CharacterSkillsSection>(id, "skills");
    }

    public CharacterRulesSection? GetRules(CharacterWorkspaceId id)
    {
        return TryParseSection<CharacterRulesSection>(id, "rules");
    }

    public CharacterBuildSection? GetBuild(CharacterWorkspaceId id)
    {
        return TryParseSection<CharacterBuildSection>(id, "build");
    }

    public CharacterMovementSection? GetMovement(CharacterWorkspaceId id)
    {
        return TryParseSection<CharacterMovementSection>(id, "movement");
    }

    public CharacterAwakeningSection? GetAwakening(CharacterWorkspaceId id)
    {
        return TryParseSection<CharacterAwakeningSection>(id, "awakening");
    }

    public CommandResult<CharacterProfileSection> UpdateMetadata(CharacterWorkspaceId id, UpdateWorkspaceMetadata command)
    {
        if (!_workspaceStore.TryGet(id, out WorkspaceDocument document))
        {
            return new CommandResult<CharacterProfileSection>(
                Success: false,
                Value: null,
                Error: "Workspace not found.");
        }

        UpdateCharacterMetadataResult result = _characterMetadataCommands.UpdateMetadata(new UpdateCharacterMetadataCommand(
            Document: new CharacterDocument(ToXmlContent(document.Content, document.Format)),
            Update: new CharacterMetadataUpdate(
                Name: command.Name,
                Alias: command.Alias,
                Notes: command.Notes)));

        _workspaceStore.Save(id, new WorkspaceDocument(result.UpdatedDocument.Content, document.Format));
        CharacterProfileSection profile = (CharacterProfileSection)_characterSectionQueries.ParseSection(
            "profile",
            new CharacterDocument(result.UpdatedDocument.Content));
        return new CommandResult<CharacterProfileSection>(
            Success: true,
            Value: profile,
            Error: null);
    }

    public CommandResult<WorkspaceSaveReceipt> Save(CharacterWorkspaceId id)
    {
        if (!_workspaceStore.TryGet(id, out WorkspaceDocument document))
        {
            return new CommandResult<WorkspaceSaveReceipt>(
                Success: false,
                Value: null,
                Error: "Workspace not found.");
        }

        return new CommandResult<WorkspaceSaveReceipt>(
                Success: true,
                Value: new WorkspaceSaveReceipt(
                    Id: id,
                    DocumentLength: document.Content.Length),
                Error: null);
    }

    public CommandResult<WorkspaceDownloadReceipt> Download(CharacterWorkspaceId id)
    {
        if (!_workspaceStore.TryGet(id, out WorkspaceDocument document))
        {
            return new CommandResult<WorkspaceDownloadReceipt>(
                Success: false,
                Value: null,
                Error: "Workspace not found.");
        }

        byte[] contentBytes = Encoding.UTF8.GetBytes(document.Content);
        string contentBase64 = Convert.ToBase64String(contentBytes);
        string fileExtension = document.Format switch
        {
            WorkspaceDocumentFormat.Chum5Xml => ".chum5",
            _ => ".dat"
        };

        return new CommandResult<WorkspaceDownloadReceipt>(
            Success: true,
            Value: new WorkspaceDownloadReceipt(
                Id: id,
                Format: document.Format,
                ContentBase64: contentBase64,
                FileName: $"{id.Value}{fileExtension}",
                DocumentLength: document.Content.Length),
            Error: null);
    }

    private static string ToXmlContent(string content, WorkspaceDocumentFormat format)
    {
        if (format != WorkspaceDocumentFormat.Chum5Xml)
            throw new InvalidOperationException($"Workspace format '{format}' is not supported.");

        if (!string.IsNullOrEmpty(content) && content[0] == '\uFEFF')
            return content[1..];

        return content;
    }

    private TSection? TryParseSection<TSection>(CharacterWorkspaceId id, string sectionId)
        where TSection : class
    {
        return GetSection(id, sectionId) as TSection;
    }
}
