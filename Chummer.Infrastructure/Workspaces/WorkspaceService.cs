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

    public WorkspaceImportResult Import(string xml)
    {
        CharacterWorkspaceId id = _workspaceStore.Create(xml);
        CharacterFileSummary summary = _characterFileQueries.ParseSummary(xml);
        return new WorkspaceImportResult(id, summary);
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

    public CommandResult<CharacterProfileSection> UpdateMetadata(CharacterWorkspaceId id, UpdateWorkspaceMetadata command)
    {
        if (!_workspaceStore.TryGet(id, out string xml))
        {
            return new CommandResult<CharacterProfileSection>(
                Success: false,
                Value: null,
                Error: "Workspace not found.");
        }

        UpdateCharacterMetadataResult result = _characterMetadataCommands.UpdateMetadata(new UpdateCharacterMetadataCommand(
            Xml: xml,
            Name: command.Name,
            Alias: command.Alias,
            Notes: command.Notes));

        _workspaceStore.Save(id, result.UpdatedXml);
        CharacterProfileSection profile = (CharacterProfileSection)_characterSectionQueries.ParseSection("profile", result.UpdatedXml);
        return new CommandResult<CharacterProfileSection>(
            Success: true,
            Value: profile,
            Error: null);
    }

    public CommandResult<string> Save(CharacterWorkspaceId id)
    {
        if (!_workspaceStore.TryGet(id, out string xml))
        {
            return new CommandResult<string>(
                Success: false,
                Value: null,
                Error: "Workspace not found.");
        }

        return new CommandResult<string>(
            Success: true,
            Value: xml,
            Error: null);
    }

    private TSection? TryParseSection<TSection>(CharacterWorkspaceId id, string sectionId)
        where TSection : class
    {
        if (!_workspaceStore.TryGet(id, out string xml))
            return null;

        return (TSection)_characterSectionQueries.ParseSection(sectionId, xml);
    }
}
