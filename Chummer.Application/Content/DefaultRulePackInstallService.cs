using Chummer.Contracts.Content;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Rulesets;

namespace Chummer.Application.Content;

public sealed class DefaultRulePackInstallService : IRulePackInstallService
{
    private readonly IRulePackInstallHistoryStore _installHistoryStore;
    private readonly IRulePackInstallStateStore _installStateStore;
    private readonly IRulePackRegistryService _rulePackRegistryService;

    public DefaultRulePackInstallService(
        IRulePackRegistryService rulePackRegistryService,
        IRulePackInstallStateStore installStateStore,
        IRulePackInstallHistoryStore installHistoryStore)
    {
        _rulePackRegistryService = rulePackRegistryService;
        _installStateStore = installStateStore;
        _installHistoryStore = installHistoryStore;
    }

    public RulePackInstallPreviewReceipt? Preview(OwnerScope owner, string packId, RuleProfileApplyTarget target, string? rulesetId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);
        ArgumentException.ThrowIfNullOrWhiteSpace(target.TargetKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(target.TargetId);

        RulePackRegistryEntry? entry = _rulePackRegistryService.Get(owner, packId, rulesetId);
        if (entry is null)
        {
            return null;
        }

        string resolvedRulesetId = ResolveRulesetId(entry, rulesetId);
        List<RulePackInstallPreviewItem> changes =
        [
            new(
                Kind: RulePackInstallPreviewChangeKinds.InstallStateChanged,
                Summary: $"Install RulePack '{entry.Manifest.PackId}' to {target.TargetKind} '{target.TargetId}'.",
                SubjectId: entry.Manifest.PackId)
        ];
        List<RuntimeInspectorWarning> warnings = BuildWarnings(entry);

        if (entry.Manifest.Capabilities.Count > 0)
        {
            changes.Add(new RulePackInstallPreviewItem(
                Kind: RulePackInstallPreviewChangeKinds.RuntimeReviewRequired,
                Summary: $"RulePack '{entry.Manifest.PackId}' contributes {entry.Manifest.Capabilities.Count} runtime capability binding(s).",
                SubjectId: entry.Manifest.PackId,
                RequiresConfirmation: true));
        }

        if (string.Equals(target.TargetKind, RuleProfileApplyTargetKinds.SessionLedger, StringComparison.Ordinal))
        {
            changes.Add(new RulePackInstallPreviewItem(
                Kind: RulePackInstallPreviewChangeKinds.SessionReplayRequired,
                Summary: "Session ledger targets may require replay or rebind after a RulePack install.",
                SubjectId: target.TargetId,
                RequiresConfirmation: true));
        }

        bool requiresConfirmation = changes.Any(change => change.RequiresConfirmation);
        return new RulePackInstallPreviewReceipt(
            PackId: entry.Manifest.PackId,
            RulesetId: resolvedRulesetId,
            Target: target,
            Changes: changes,
            Warnings: warnings,
            RequiresConfirmation: requiresConfirmation);
    }

    public RulePackInstallReceipt? Apply(OwnerScope owner, string packId, RuleProfileApplyTarget target, string? rulesetId = null)
    {
        RulePackInstallPreviewReceipt? preview = Preview(owner, packId, target, rulesetId);
        if (preview is null)
        {
            return null;
        }

        RulePackRegistryEntry? entry = _rulePackRegistryService.Get(owner, packId, preview.RulesetId);
        if (entry is null)
        {
            return null;
        }

        ArtifactInstallState current = entry.Install;
        string desiredState = ResolveInstallState(target);
        if (string.Equals(current.State, desiredState, StringComparison.Ordinal)
            && string.Equals(current.InstalledTargetKind, target.TargetKind, StringComparison.Ordinal)
            && string.Equals(current.InstalledTargetId, target.TargetId, StringComparison.Ordinal))
        {
            return new RulePackInstallReceipt(
                PackId: preview.PackId,
                RulesetId: preview.RulesetId,
                Target: target,
                Outcome: RulePackInstallOutcomes.AlreadyInstalled,
                Install: current,
                Preview: preview);
        }

        DateTimeOffset appliedAtUtc = DateTimeOffset.UtcNow;
        ArtifactInstallState install = new(
            State: desiredState,
            InstalledAtUtc: appliedAtUtc,
            InstalledTargetKind: target.TargetKind,
            InstalledTargetId: target.TargetId,
            RuntimeFingerprint: current.RuntimeFingerprint);
        ArtifactInstallState persistedInstall = _installStateStore.Upsert(
            owner,
            new RulePackInstallRecord(entry.Manifest.PackId, entry.Manifest.Version, preview.RulesetId, install)).Install;
        _installHistoryStore.Append(
            owner,
            new RulePackInstallHistoryRecord(
                entry.Manifest.PackId,
                entry.Manifest.Version,
                preview.RulesetId,
                new ArtifactInstallHistoryEntry(
                    Operation: string.Equals(desiredState, ArtifactInstallStates.Pinned, StringComparison.Ordinal)
                        ? ArtifactInstallHistoryOperations.Pin
                        : ArtifactInstallHistoryOperations.Install,
                    Install: persistedInstall,
                    AppliedAtUtc: appliedAtUtc)));

        return new RulePackInstallReceipt(
            PackId: preview.PackId,
            RulesetId: preview.RulesetId,
            Target: target,
            Outcome: RulePackInstallOutcomes.Applied,
            Install: persistedInstall,
            Preview: preview);
    }

    private static string ResolveInstallState(RuleProfileApplyTarget target)
    {
        return string.Equals(target.TargetKind, RuleProfileApplyTargetKinds.GlobalDefaults, StringComparison.Ordinal)
            ? ArtifactInstallStates.Pinned
            : ArtifactInstallStates.Installed;
    }

    private static string ResolveRulesetId(RulePackRegistryEntry entry, string? rulesetId)
    {
        return RulesetDefaults.NormalizeOptional(rulesetId)
            ?? entry.Manifest.Targets.FirstOrDefault()
            ?? throw new InvalidOperationException($"RulePack '{entry.Manifest.PackId}' did not declare any target rulesets.");
    }

    private static List<RuntimeInspectorWarning> BuildWarnings(RulePackRegistryEntry entry)
    {
        List<RuntimeInspectorWarning> warnings = [];
        if (string.Equals(entry.Publication.Visibility, ArtifactVisibilityModes.LocalOnly, StringComparison.Ordinal))
        {
            warnings.Add(new RuntimeInspectorWarning(
                Kind: RuntimeInspectorWarningKinds.Trust,
                Severity: RuntimeInspectorWarningSeverityLevels.Info,
                Message: "RulePack is local-only and will need publishing or export before other owners can reuse it.",
                SubjectId: entry.Manifest.PackId));
        }

        if (entry.Manifest.Capabilities.Count == 0)
        {
            warnings.Add(new RuntimeInspectorWarning(
                Kind: RuntimeInspectorWarningKinds.ProviderBinding,
                Severity: RuntimeInspectorWarningSeverityLevels.Info,
                Message: "RulePack installs as content-only data without additional capability bindings.",
                SubjectId: entry.Manifest.PackId));
        }

        return warnings;
    }
}
