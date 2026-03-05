using Chummer.Contracts.Api;
using Chummer.Application.Characters;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;
using Microsoft.Extensions.DependencyInjection;

namespace Chummer.Infrastructure.Workspaces;

public sealed class WorkspaceService : IWorkspaceService
{
    private readonly IWorkspaceStore _workspaceStore;
    private readonly IRulesetWorkspaceCodecResolver _workspaceCodecResolver;

    [ActivatorUtilitiesConstructor]
    public WorkspaceService(
        IWorkspaceStore workspaceStore,
        IRulesetWorkspaceCodecResolver workspaceCodecResolver)
    {
        _workspaceStore = workspaceStore;
        _workspaceCodecResolver = workspaceCodecResolver;
    }

    private WorkspaceService(
        IWorkspaceStore workspaceStore,
        ICharacterFileQueries characterFileQueries,
        ICharacterSectionQueries characterSectionQueries,
        ICharacterMetadataCommands characterMetadataCommands)
        : this(
            workspaceStore,
            new RulesetWorkspaceCodecResolver(
            [
                new Sr5WorkspaceCodec(
                    characterFileQueries,
                    characterSectionQueries,
                    characterMetadataCommands)
            ]))
    {
    }

    public WorkspaceImportResult Import(WorkspaceImportDocument document)
    {
        string rulesetId = RulesetDefaults.Normalize(document.RulesetId);
        IRulesetWorkspaceCodec codec = _workspaceCodecResolver.Resolve(rulesetId);
        WorkspacePayloadEnvelope envelope = codec.WrapImport(rulesetId, document);
        CharacterFileSummary summary = codec.ParseSummary(envelope);

        CharacterWorkspaceId id = _workspaceStore.Create(new WorkspaceDocument(
            Content: envelope.Payload,
            Format: document.Format,
            RulesetId: envelope.RulesetId,
            PayloadEnvelope: envelope));
        return new WorkspaceImportResult(id, summary, envelope.RulesetId);
    }

    public IReadOnlyList<WorkspaceListItem> List(int? maxCount = null)
    {
        List<WorkspaceListItem> workspaces = [];
        int? normalizedMaxCount = maxCount is > 0 ? maxCount : null;

        foreach (WorkspaceStoreEntry entry in _workspaceStore.List())
        {
            if (normalizedMaxCount is not null && workspaces.Count >= normalizedMaxCount.Value)
            {
                break;
            }

            CharacterWorkspaceId id = entry.Id;
            if (!_workspaceStore.TryGet(id, out WorkspaceDocument document))
            {
                continue;
            }

            WorkspacePayloadEnvelope envelope = ResolveEnvelope(document);
            CharacterFileSummary summary;
            try
            {
                IRulesetWorkspaceCodec codec = _workspaceCodecResolver.Resolve(envelope.RulesetId);
                summary = codec.ParseSummary(envelope);
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
                RulesetId: envelope.RulesetId));
        }

        return workspaces;
    }

    public bool Close(CharacterWorkspaceId id)
    {
        return _workspaceStore.Delete(id);
    }

    public object? GetSection(CharacterWorkspaceId id, string sectionId)
    {
        if (!TryResolveEnvelope(id, out WorkspacePayloadEnvelope envelope))
        {
            return null;
        }

        IRulesetWorkspaceCodec codec = _workspaceCodecResolver.Resolve(envelope.RulesetId);
        return codec.ParseSection(sectionId, envelope);
    }

    public CharacterFileSummary? GetSummary(CharacterWorkspaceId id)
    {
        if (!TryResolveEnvelope(id, out WorkspacePayloadEnvelope envelope))
        {
            return null;
        }

        IRulesetWorkspaceCodec codec = _workspaceCodecResolver.Resolve(envelope.RulesetId);
        return codec.ParseSummary(envelope);
    }

    public CharacterValidationResult? Validate(CharacterWorkspaceId id)
    {
        if (!TryResolveEnvelope(id, out WorkspacePayloadEnvelope envelope))
        {
            return null;
        }

        IRulesetWorkspaceCodec codec = _workspaceCodecResolver.Resolve(envelope.RulesetId);
        return codec.Validate(envelope);
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

        WorkspacePayloadEnvelope envelope = ResolveEnvelope(document);
        IRulesetWorkspaceCodec codec = _workspaceCodecResolver.Resolve(envelope.RulesetId);
        WorkspacePayloadEnvelope updatedEnvelope = codec.UpdateMetadata(envelope, command);

        _workspaceStore.Save(id, CreateUpdatedDocument(document, updatedEnvelope));

        CharacterProfileSection? profile = codec.ParseSection("profile", updatedEnvelope) as CharacterProfileSection;
        if (profile is null)
        {
            return new CommandResult<CharacterProfileSection>(
                Success: false,
                Value: null,
                Error: "Profile section was not available after metadata update.");
        }

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
        IRulesetWorkspaceCodec codec = _workspaceCodecResolver.Resolve(envelope.RulesetId);
        WorkspaceDownloadReceipt receipt = codec.BuildDownload(id, envelope, document.Format);

        return new CommandResult<WorkspaceDownloadReceipt>(
            Success: true,
            Value: receipt,
            Error: null);
    }

    public CommandResult<DataExportBundle> Export(CharacterWorkspaceId id)
    {
        if (!_workspaceStore.TryGet(id, out WorkspaceDocument document))
        {
            return new CommandResult<DataExportBundle>(
                Success: false,
                Value: null,
                Error: "Workspace not found.");
        }

        WorkspacePayloadEnvelope envelope = ResolveEnvelope(document);
        IRulesetWorkspaceCodec codec = _workspaceCodecResolver.Resolve(envelope.RulesetId);
        DataExportBundle bundle = new(
            Summary: codec.ParseSummary(envelope),
            Profile: TryParseExportSection<CharacterProfileSection>(codec, envelope, "profile"),
            Progress: TryParseExportSection<CharacterProgressSection>(codec, envelope, "progress"),
            Attributes: TryParseExportSection<CharacterAttributesSection>(codec, envelope, "attributes"),
            Skills: TryParseExportSection<CharacterSkillsSection>(codec, envelope, "skills"),
            Inventory: TryParseExportSection<CharacterInventorySection>(codec, envelope, "inventory"),
            Qualities: TryParseExportSection<CharacterQualitiesSection>(codec, envelope, "qualities"),
            Contacts: TryParseExportSection<CharacterContactsSection>(codec, envelope, "contacts"));

        return new CommandResult<DataExportBundle>(
            Success: true,
            Value: bundle,
            Error: null);
    }

    private TSection? TryParseSection<TSection>(CharacterWorkspaceId id, string sectionId)
        where TSection : class
    {
        return GetSection(id, sectionId) as TSection;
    }

    private static TSection? TryParseExportSection<TSection>(
        IRulesetWorkspaceCodec codec,
        WorkspacePayloadEnvelope envelope,
        string sectionId)
        where TSection : class
    {
        try
        {
            return codec.ParseSection(sectionId, envelope) as TSection;
        }
        catch
        {
            return null;
        }
    }

    private bool TryResolveEnvelope(CharacterWorkspaceId id, out WorkspacePayloadEnvelope envelope)
    {
        if (!_workspaceStore.TryGet(id, out WorkspaceDocument document))
        {
            envelope = default!;
            return false;
        }

        envelope = ResolveEnvelope(document);
        return true;
    }

    private WorkspacePayloadEnvelope ResolveEnvelope(WorkspaceDocument document)
    {
        WorkspacePayloadEnvelope? existing = document.PayloadEnvelope;
        string normalizedRulesetId = RulesetDefaults.Normalize(
            existing?.RulesetId ?? document.RulesetId);
        IRulesetWorkspaceCodec codec = _workspaceCodecResolver.Resolve(normalizedRulesetId);
        int schemaVersion = existing?.SchemaVersion is > 0
            ? existing.SchemaVersion
            : codec.SchemaVersion;
        string payloadKind = string.IsNullOrWhiteSpace(existing?.PayloadKind)
            ? codec.PayloadKind
            : existing.PayloadKind;
        string payload = existing?.Payload ?? document.Content;
        return new WorkspacePayloadEnvelope(
            RulesetId: normalizedRulesetId,
            SchemaVersion: schemaVersion,
            PayloadKind: payloadKind,
            Payload: payload);
    }

    private static WorkspaceDocument CreateUpdatedDocument(WorkspaceDocument current, WorkspacePayloadEnvelope updatedEnvelope)
    {
        return new WorkspaceDocument(
            Content: updatedEnvelope.Payload,
            Format: current.Format,
            RulesetId: updatedEnvelope.RulesetId,
            PayloadEnvelope: updatedEnvelope);
    }
}
