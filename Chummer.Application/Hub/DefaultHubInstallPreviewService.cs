using Chummer.Application.Content;
using Chummer.Contracts.Content;
using Chummer.Contracts.Hub;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Rulesets;

namespace Chummer.Application.Hub;

public sealed class DefaultHubInstallPreviewService : IHubInstallPreviewService
{
    private readonly IRulesetPluginRegistry _rulesetPluginRegistry;
    private readonly IRuleProfileApplicationService _ruleProfileApplicationService;
    private readonly IRuntimeLockRegistryService _runtimeLockRegistryService;
    private readonly IRulePackRegistryService _rulePackRegistryService;
    private readonly IBuildKitRegistryService _buildKitRegistryService;

    public DefaultHubInstallPreviewService(
        IRulesetPluginRegistry rulesetPluginRegistry,
        IRuleProfileApplicationService ruleProfileApplicationService,
        IRuntimeLockRegistryService runtimeLockRegistryService,
        IRulePackRegistryService rulePackRegistryService,
        IBuildKitRegistryService buildKitRegistryService)
    {
        _rulesetPluginRegistry = rulesetPluginRegistry;
        _ruleProfileApplicationService = ruleProfileApplicationService;
        _runtimeLockRegistryService = runtimeLockRegistryService;
        _rulePackRegistryService = rulePackRegistryService;
        _buildKitRegistryService = buildKitRegistryService;
    }

    public HubProjectInstallPreviewReceipt? Preview(OwnerScope owner, string kind, string itemId, RuleProfileApplyTarget target, string? rulesetId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(target.TargetKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(target.TargetId);

        return kind.Trim() switch
        {
            HubCatalogItemKinds.RuleProfile => PreviewRuleProfile(owner, itemId, target, rulesetId),
            HubCatalogItemKinds.RuntimeLock => PreviewRuntimeLock(owner, itemId, target, rulesetId),
            HubCatalogItemKinds.RulePack => PreviewRulePack(owner, itemId, target, rulesetId),
            HubCatalogItemKinds.BuildKit => PreviewBuildKit(owner, itemId, target, rulesetId),
            _ => null
        };
    }

    private HubProjectInstallPreviewReceipt? PreviewRuleProfile(OwnerScope owner, string itemId, RuleProfileApplyTarget target, string? rulesetId)
    {
        RuleProfilePreviewReceipt? preview = _ruleProfileApplicationService.Preview(owner, itemId, target, rulesetId);
        if (preview is null)
        {
            return null;
        }

        return new HubProjectInstallPreviewReceipt(
            Kind: HubCatalogItemKinds.RuleProfile,
            ItemId: itemId,
            Target: target,
            State: HubProjectInstallPreviewStates.Ready,
            Changes: preview.Changes.Select(change => new HubProjectInstallPreviewChange(change.Kind, change.Summary, change.SubjectId ?? itemId, change.RequiresConfirmation)).ToArray(),
            Diagnostics: preview.Warnings.Select(warning => new HubProjectInstallPreviewDiagnostic(warning.Kind, warning.Severity, warning.Message, warning.SubjectId)).ToArray(),
            RuntimeFingerprint: preview.RuntimeLock.RuntimeFingerprint,
            RequiresConfirmation: preview.RequiresConfirmation);
    }

    private HubProjectInstallPreviewReceipt? PreviewRuntimeLock(OwnerScope owner, string itemId, RuleProfileApplyTarget target, string? rulesetId)
    {
        RuntimeLockRegistryEntry? entry = _runtimeLockRegistryService.Get(owner, itemId, rulesetId);
        if (entry is null)
        {
            return null;
        }

        List<HubProjectInstallPreviewChange> changes =
        [
            new(
                Kind: HubProjectInstallPreviewChangeKinds.RuntimeLockPinned,
                Summary: $"Pin runtime '{entry.RuntimeLock.RuntimeFingerprint}' to {target.TargetKind} '{target.TargetId}'.",
                SubjectId: entry.LockId)
        ];
        List<HubProjectInstallPreviewDiagnostic> diagnostics = [];

        if (entry.RuntimeLock.RulePacks.Count == 0)
        {
            diagnostics.Add(new HubProjectInstallPreviewDiagnostic(
                Kind: HubProjectInstallPreviewDiagnosticKinds.ProviderBinding,
                Severity: HubProjectInstallPreviewDiagnosticSeverityLevels.Info,
                Message: "Runtime lock resolves to built-in content without additional RulePacks.",
                SubjectId: entry.LockId));
        }

        if (string.Equals(target.TargetKind, RuleProfileApplyTargetKinds.SessionLedger, StringComparison.Ordinal))
        {
            changes.Add(new HubProjectInstallPreviewChange(
                Kind: HubProjectInstallPreviewChangeKinds.SessionReplayRequired,
                Summary: "Session ledger targets may require replay or rebind after a runtime-lock change.",
                SubjectId: target.TargetId,
                RequiresConfirmation: true));
        }

        return new HubProjectInstallPreviewReceipt(
            Kind: HubCatalogItemKinds.RuntimeLock,
            ItemId: itemId,
            Target: target,
            State: HubProjectInstallPreviewStates.Ready,
            Changes: changes,
            Diagnostics: diagnostics,
            RuntimeFingerprint: entry.RuntimeLock.RuntimeFingerprint,
            RequiresConfirmation: changes.Any(change => change.RequiresConfirmation));
    }

    private HubProjectInstallPreviewReceipt? PreviewRulePack(OwnerScope owner, string itemId, RuleProfileApplyTarget target, string? rulesetId)
    {
        foreach (string candidateRulesetId in EnumerateRulesetIds(rulesetId))
        {
            RulePackRegistryEntry? entry = _rulePackRegistryService.Get(owner, itemId, candidateRulesetId);
            if (entry is null)
            {
                continue;
            }

            return CreateDeferredReceipt(
                kind: HubCatalogItemKinds.RulePack,
                itemId: itemId,
                target: target,
                deferredReason: "hub_rulepack_install_preview_not_implemented",
                message: $"RulePack install preview is not implemented yet for '{entry.Manifest.PackId}'.");
        }

        return null;
    }

    private HubProjectInstallPreviewReceipt? PreviewBuildKit(OwnerScope owner, string itemId, RuleProfileApplyTarget target, string? rulesetId)
    {
        foreach (string candidateRulesetId in EnumerateRulesetIds(rulesetId))
        {
            BuildKitRegistryEntry? entry = _buildKitRegistryService.Get(owner, itemId, candidateRulesetId);
            if (entry is null)
            {
                continue;
            }

            return CreateDeferredReceipt(
                kind: HubCatalogItemKinds.BuildKit,
                itemId: itemId,
                target: target,
                deferredReason: "hub_buildkit_apply_preview_not_implemented",
                message: $"BuildKit apply preview is not implemented yet for '{entry.Manifest.BuildKitId}'.");
        }

        return null;
    }

    private IEnumerable<string> EnumerateRulesetIds(string? rulesetId)
    {
        string? normalizedRulesetId = RulesetDefaults.NormalizeOptional(rulesetId);
        if (normalizedRulesetId is not null)
        {
            yield return normalizedRulesetId;
            yield break;
        }

        foreach (IRulesetPlugin plugin in _rulesetPluginRegistry.All)
        {
            yield return plugin.Id.NormalizedValue;
        }
    }

    private static HubProjectInstallPreviewReceipt CreateDeferredReceipt(
        string kind,
        string itemId,
        RuleProfileApplyTarget target,
        string deferredReason,
        string message)
    {
        return new HubProjectInstallPreviewReceipt(
            Kind: kind,
            ItemId: itemId,
            Target: target,
            State: HubProjectInstallPreviewStates.Deferred,
            Changes:
            [
                new HubProjectInstallPreviewChange(
                    Kind: HubProjectInstallPreviewChangeKinds.InstallDeferred,
                    Summary: message,
                    SubjectId: itemId)
            ],
            Diagnostics:
            [
                new HubProjectInstallPreviewDiagnostic(
                    Kind: HubProjectInstallPreviewDiagnosticKinds.Installability,
                    Severity: HubProjectInstallPreviewDiagnosticSeverityLevels.Info,
                    Message: message,
                    SubjectId: itemId)
            ],
            DeferredReason: deferredReason);
    }
}
