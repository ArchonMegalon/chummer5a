using Chummer.Application.Characters;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Workspaces;

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
        CharacterWorkspaceId id = _workspaceStore.Create(new WorkspaceDocument(xml, document.Format));
        CharacterFileSummary summary = _characterFileQueries.ParseSummary(new CharacterXmlDocument(xml));
        return new WorkspaceImportResult(id, summary);
    }

    public object? GetSection(CharacterWorkspaceId id, string sectionId)
    {
        if (!_workspaceStore.TryGet(id, out WorkspaceDocument document))
            return null;

        string xml = ToXmlContent(document.Content, document.Format);
        return _characterSectionQueries.ParseSection(sectionId, new CharacterXmlDocument(xml));
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
            Xml: ToXmlContent(document.Content, document.Format),
            Name: command.Name,
            Alias: command.Alias,
            Notes: command.Notes));

        _workspaceStore.Save(id, new WorkspaceDocument(result.UpdatedXml, document.Format));
        CharacterProfileSection profile = (CharacterProfileSection)_characterSectionQueries.ParseSection(
            "profile",
            new CharacterXmlDocument(result.UpdatedXml));
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

    private static string ToXmlContent(string content, WorkspaceDocumentFormat format)
    {
        if (format != WorkspaceDocumentFormat.Chum5Xml)
            throw new InvalidOperationException($"Workspace format '{format}' is not supported.");

        return content;
    }

    private TSection? TryParseSection<TSection>(CharacterWorkspaceId id, string sectionId)
        where TSection : class
    {
        return GetSection(id, sectionId) as TSection;
    }
}
