using Chummer.Contracts.Characters;
using Chummer.Contracts.Workspaces;

namespace Chummer.Application.Workspaces;

public interface IRulesetWorkspaceCodec
{
    string RulesetId { get; }

    string PayloadKind { get; }

    WorkspacePayloadEnvelope WrapImport(string rulesetId, WorkspaceImportDocument document);

    CharacterFileSummary ParseSummary(WorkspacePayloadEnvelope envelope);

    object ParseSection(string sectionId, WorkspacePayloadEnvelope envelope);

    CharacterValidationResult Validate(WorkspacePayloadEnvelope envelope);

    WorkspacePayloadEnvelope UpdateMetadata(WorkspacePayloadEnvelope envelope, UpdateWorkspaceMetadata command);
}
