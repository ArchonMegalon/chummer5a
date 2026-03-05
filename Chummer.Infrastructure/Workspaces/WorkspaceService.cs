using Chummer.Application.Characters;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;
using System.Text;

namespace Chummer.Infrastructure.Workspaces;

public sealed class WorkspaceService : IWorkspaceService
{
    private const int DefaultEnvelopeSchemaVersion = 1;
    private const string DefaultEnvelopePayloadKind = "workspace";
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
        string rulesetId = RulesetDefaults.Normalize(document.RulesetId);
        string xml = ToXmlContent(document.Content, document.Format);
        CharacterFileSummary summary = _characterFileQueries.ParseSummary(new CharacterDocument(xml));
        WorkspacePayloadEnvelope envelope = new(
            RulesetId: rulesetId,
            SchemaVersion: DefaultEnvelopeSchemaVersion,
            PayloadKind: DefaultEnvelopePayloadKind,
            Payload: xml);
        CharacterWorkspaceId id = _workspaceStore.Create(new WorkspaceDocument(
            Content: xml,
            Format: document.Format,
            RulesetId: rulesetId,
            PayloadEnvelope: envelope));
        return new WorkspaceImportResult(id, summary, rulesetId);
    }

    public IReadOnlyList<WorkspaceListItem> List(int? maxCount = null)
    {
        List<WorkspaceListItem> workspaces = [];
        int? normalizedMaxCount = maxCount is > 0 ? maxCount : null;

        foreach (WorkspaceStoreEntry entry in _workspaceStore.List())
        {
            if (normalizedMaxCount is not null && workspaces.Count >= normalizedMaxCount.Value)
                break;

            CharacterWorkspaceId id = entry.Id;
            if (!_workspaceStore.TryGet(id, out WorkspaceDocument document))
                continue;

            CharacterFileSummary summary;
            try
            {
                summary = _characterFileQueries.ParseSummary(new CharacterDocument(ResolveXmlPayload(document)));
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
                LastUpdatedUtc: entry.LastUpdatedUtc,
                RulesetId: ResolveEnvelope(document).RulesetId));
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

        string xml = ResolveXmlPayload(document);
        return _characterSectionQueries.ParseSection(sectionId, new CharacterDocument(xml));
    }

    public CharacterFileSummary? GetSummary(CharacterWorkspaceId id)
    {
        if (!_workspaceStore.TryGet(id, out WorkspaceDocument document))
            return null;

        string xml = ResolveXmlPayload(document);
        return _characterFileQueries.ParseSummary(new CharacterDocument(xml));
    }

    public CharacterValidationResult? Validate(CharacterWorkspaceId id)
    {
        if (!_workspaceStore.TryGet(id, out WorkspaceDocument document))
            return null;

        string xml = ResolveXmlPayload(document);
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
            Document: new CharacterDocument(ResolveXmlPayload(document)),
            Update: new CharacterMetadataUpdate(
                Name: command.Name,
                Alias: command.Alias,
                Notes: command.Notes)));

        _workspaceStore.Save(id, CreateUpdatedDocument(document, result.UpdatedDocument.Content));
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

        WorkspacePayloadEnvelope envelope = ResolveEnvelope(document);
        return new CommandResult<WorkspaceSaveReceipt>(
                Success: true,
                Value: new WorkspaceSaveReceipt(
                    Id: id,
                    DocumentLength: envelope.Payload.Length,
                    RulesetId: envelope.RulesetId),
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

        WorkspacePayloadEnvelope envelope = ResolveEnvelope(document);
        byte[] contentBytes = Encoding.UTF8.GetBytes(envelope.Payload);
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
                DocumentLength: envelope.Payload.Length,
                RulesetId: envelope.RulesetId),
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

    private static string ResolveXmlPayload(WorkspaceDocument document)
    {
        WorkspacePayloadEnvelope envelope = ResolveEnvelope(document);
        return ToXmlContent(envelope.Payload, document.Format);
    }

    private static WorkspacePayloadEnvelope ResolveEnvelope(WorkspaceDocument document)
    {
        WorkspacePayloadEnvelope? existing = document.PayloadEnvelope;
        string normalizedRulesetId = RulesetDefaults.Normalize(
            existing?.RulesetId ?? document.RulesetId);
        int schemaVersion = existing?.SchemaVersion is > 0
            ? existing.SchemaVersion
            : DefaultEnvelopeSchemaVersion;
        string payloadKind = string.IsNullOrWhiteSpace(existing?.PayloadKind)
            ? DefaultEnvelopePayloadKind
            : existing.PayloadKind;
        string payload = existing?.Payload ?? document.Content;
        return new WorkspacePayloadEnvelope(
            RulesetId: normalizedRulesetId,
            SchemaVersion: schemaVersion,
            PayloadKind: payloadKind,
            Payload: payload);
    }

    private static WorkspaceDocument CreateUpdatedDocument(WorkspaceDocument current, string payload)
    {
        WorkspacePayloadEnvelope existingEnvelope = ResolveEnvelope(current);
        WorkspacePayloadEnvelope updatedEnvelope = existingEnvelope with
        {
            Payload = payload
        };
        return new WorkspaceDocument(
            Content: payload,
            Format: current.Format,
            RulesetId: updatedEnvelope.RulesetId,
            PayloadEnvelope: updatedEnvelope);
    }
}
