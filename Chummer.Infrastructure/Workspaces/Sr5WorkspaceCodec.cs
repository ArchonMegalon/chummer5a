using Chummer.Application.Characters;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;

namespace Chummer.Infrastructure.Workspaces;

public sealed class Sr5WorkspaceCodec : IRulesetWorkspaceCodec
{
    public const int SchemaVersion = 1;
    public const string Sr5PayloadKind = "sr5/chum5-xml";
    private readonly ICharacterFileQueries _characterFileQueries;
    private readonly ICharacterSectionQueries _characterSectionQueries;
    private readonly ICharacterMetadataCommands _characterMetadataCommands;

    public Sr5WorkspaceCodec(
        ICharacterFileQueries characterFileQueries,
        ICharacterSectionQueries characterSectionQueries,
        ICharacterMetadataCommands characterMetadataCommands)
    {
        _characterFileQueries = characterFileQueries;
        _characterSectionQueries = characterSectionQueries;
        _characterMetadataCommands = characterMetadataCommands;
    }

    public string RulesetId => RulesetDefaults.Sr5;

    public string PayloadKind => Sr5PayloadKind;

    public WorkspacePayloadEnvelope WrapImport(string rulesetId, WorkspaceImportDocument document)
    {
        string normalizedRulesetId = RulesetDefaults.Normalize(rulesetId);
        string xml = ToXmlContent(document.Content, document.Format);
        return new WorkspacePayloadEnvelope(
            RulesetId: normalizedRulesetId,
            SchemaVersion: SchemaVersion,
            PayloadKind: PayloadKind,
            Payload: xml);
    }

    public CharacterFileSummary ParseSummary(WorkspacePayloadEnvelope envelope)
    {
        return _characterFileQueries.ParseSummary(new CharacterDocument(ToXmlContent(envelope.Payload, WorkspaceDocumentFormat.Chum5Xml)));
    }

    public object ParseSection(string sectionId, WorkspacePayloadEnvelope envelope)
    {
        return _characterSectionQueries.ParseSection(sectionId, new CharacterDocument(ToXmlContent(envelope.Payload, WorkspaceDocumentFormat.Chum5Xml)));
    }

    public CharacterValidationResult Validate(WorkspacePayloadEnvelope envelope)
    {
        return _characterFileQueries.Validate(new CharacterDocument(ToXmlContent(envelope.Payload, WorkspaceDocumentFormat.Chum5Xml)));
    }

    public WorkspacePayloadEnvelope UpdateMetadata(WorkspacePayloadEnvelope envelope, UpdateWorkspaceMetadata command)
    {
        UpdateCharacterMetadataResult result = _characterMetadataCommands.UpdateMetadata(new UpdateCharacterMetadataCommand(
            Document: new CharacterDocument(ToXmlContent(envelope.Payload, WorkspaceDocumentFormat.Chum5Xml)),
            Update: new CharacterMetadataUpdate(
                Name: command.Name,
                Alias: command.Alias,
                Notes: command.Notes)));

        return envelope with
        {
            SchemaVersion = envelope.SchemaVersion > 0 ? envelope.SchemaVersion : SchemaVersion,
            PayloadKind = string.IsNullOrWhiteSpace(envelope.PayloadKind) ? PayloadKind : envelope.PayloadKind,
            Payload = result.UpdatedDocument.Content
        };
    }

    private static string ToXmlContent(string content, WorkspaceDocumentFormat format)
    {
        if (format != WorkspaceDocumentFormat.Chum5Xml)
        {
            throw new InvalidOperationException($"Workspace format '{format}' is not supported.");
        }

        if (!string.IsNullOrEmpty(content) && content[0] == '\uFEFF')
        {
            return content[1..];
        }

        return content;
    }
}
