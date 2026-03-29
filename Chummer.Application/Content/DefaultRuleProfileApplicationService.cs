using System.Linq;
using Chummer.Contracts.Content;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Rulesets;

namespace Chummer.Application.Content;

public sealed class DefaultRuleProfileApplicationService : IRuleProfileApplicationService
{
    private readonly IRuleProfileInstallHistoryStore _installHistoryStore;
    private readonly IRuleProfileInstallStateStore _installStateStore;
    private readonly IRuleProfileRegistryService _ruleProfileRegistryService;
    private readonly IRuntimeLockInstallService _runtimeLockInstallService;

    public DefaultRuleProfileApplicationService(
        IRuleProfileRegistryService ruleProfileRegistryService,
        IRuntimeLockInstallService runtimeLockInstallService,
        IRuleProfileInstallStateStore installStateStore,
        IRuleProfileInstallHistoryStore installHistoryStore)
    {
        _ruleProfileRegistryService = ruleProfileRegistryService;
        _runtimeLockInstallService = runtimeLockInstallService;
        _installStateStore = installStateStore;
        _installHistoryStore = installHistoryStore;
    }

    public RuleProfilePreviewReceipt? Preview(OwnerScope owner, string profileId, RuleProfileApplyTarget target, string? rulesetId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(target.TargetKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(target.TargetId);

        RuleProfileRegistryEntry? entry = _ruleProfileRegistryService.Get(owner, profileId, rulesetId);
        return entry is null ? null : CreatePreview(entry, target);
    }

    public RuleProfileApplyReceipt? Apply(OwnerScope owner, string profileId, RuleProfileApplyTarget target, string? rulesetId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(target.TargetKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(target.TargetId);

        RuleProfileRegistryEntry? entry = _ruleProfileRegistryService.Get(owner, profileId, rulesetId);
        if (entry is null)
        {
            return null;
        }

        RuleProfilePreviewReceipt preview = CreatePreview(entry, target);
        string resolvedRulesetId = RulesetDefaults.NormalizeOptional(rulesetId) ?? entry.Manifest.RulesetId;
        RuntimeLockInstallReceipt? installReceipt = _runtimeLockInstallService.Apply(
            owner,
            entry.Manifest.RuntimeLock.RuntimeFingerprint,
            target,
            resolvedRulesetId);
        if (installReceipt is null)
        {
            return new RuleProfileApplyReceipt(
                ProfileId: preview.ProfileId,
                Target: preview.Target,
                Outcome: RuleProfileApplyOutcomes.Blocked,
                Preview: preview);
        }

        if (string.Equals(installReceipt.Outcome, RuntimeLockInstallOutcomes.Blocked, StringComparison.Ordinal))
        {
            return new RuleProfileApplyReceipt(
                ProfileId: preview.ProfileId,
                Target: preview.Target,
                Outcome: RuleProfileApplyOutcomes.Blocked,
                Preview: preview,
                InstallReceipt: installReceipt);
        }

        PersistProfileInstall(owner, entry, target, resolvedRulesetId, installReceipt);

        return new RuleProfileApplyReceipt(
            ProfileId: preview.ProfileId,
            Target: preview.Target,
            Outcome: RuleProfileApplyOutcomes.Applied,
            Preview: preview,
            InstallReceipt: installReceipt);
    }

    private RuleProfilePreviewReceipt CreatePreview(RuleProfileRegistryEntry entry, RuleProfileApplyTarget target)
    {
        RuleProfilePreviewItem[] changes = BuildPreviewChanges(entry, target);
        RuntimeInspectorWarning[] warnings = BuildWarnings(entry);
        ArtifactInstallState install = RuntimeInspectorPromotionNarrator.NormalizeInstall(entry.Install, entry.Manifest.RuntimeLock.RuntimeFingerprint);
        RuntimeInspectorPromotionProjection promotion = RuntimeInspectorPromotionNarrator.BuildPromotion(entry, install);

        return new RuleProfilePreviewReceipt(
            ProfileId: entry.Manifest.ProfileId,
            Target: target,
            RuntimeLock: entry.Manifest.RuntimeLock,
            Changes: changes,
            Warnings: warnings,
            RequiresConfirmation: changes.Any(change => change.RequiresConfirmation),
            Promotion: promotion);
    }

    private void PersistProfileInstall(
        OwnerScope owner,
        RuleProfileRegistryEntry entry,
        RuleProfileApplyTarget target,
        string resolvedRulesetId,
        RuntimeLockInstallReceipt installReceipt)
    {
        ArtifactInstallState current = entry.Install;
        string runtimeFingerprint = installReceipt.RuntimeLock.RuntimeFingerprint;

        if (string.Equals(current.State, ArtifactInstallStates.Pinned, StringComparison.Ordinal)
            && string.Equals(current.InstalledTargetKind, target.TargetKind, StringComparison.Ordinal)
            && string.Equals(current.InstalledTargetId, target.TargetId, StringComparison.Ordinal)
            && string.Equals(current.RuntimeFingerprint, runtimeFingerprint, StringComparison.Ordinal)
            && current.InstalledAtUtc is not null)
        {
            return;
        }

        DateTimeOffset appliedAtUtc = installReceipt.InstalledAtUtc;
        ArtifactInstallState install = new(
            State: ArtifactInstallStates.Pinned,
            InstalledAtUtc: current.InstalledAtUtc ?? appliedAtUtc,
            InstalledTargetKind: target.TargetKind,
            InstalledTargetId: target.TargetId,
            RuntimeFingerprint: runtimeFingerprint);
        ArtifactInstallState persistedInstall = _installStateStore.Upsert(
            owner,
            new RuleProfileInstallRecord(
                entry.Manifest.ProfileId,
                resolvedRulesetId,
                install)).Install;
        _installHistoryStore.Append(
            owner,
            new RuleProfileInstallHistoryRecord(
                entry.Manifest.ProfileId,
                resolvedRulesetId,
                new ArtifactInstallHistoryEntry(
                    Operation: ArtifactInstallHistoryOperations.Pin,
                    Install: persistedInstall,
                    AppliedAtUtc: appliedAtUtc)));
    }

    private static RuleProfilePreviewItem[] BuildPreviewChanges(RuleProfileRegistryEntry entry, RuleProfileApplyTarget target)
    {
        List<RuleProfilePreviewItem> changes =
        [
            new(
                Kind: RuleProfilePreviewChangeKinds.RuntimeLockPinned,
                Summary: $"Pin runtime '{entry.Manifest.RuntimeLock.RuntimeFingerprint}' to {target.TargetKind} '{target.TargetId}'.",
                SubjectId: entry.Manifest.RuntimeLock.RuntimeFingerprint,
                RequiresConfirmation: false)
        ];

        if (entry.Manifest.RulePacks.Count > 0)
        {
            changes.Add(new RuleProfilePreviewItem(
                Kind: RuleProfilePreviewChangeKinds.RulePackSelectionChanged,
                Summary: $"Apply {entry.Manifest.RulePacks.Count} RulePack selection(s) from profile '{entry.Manifest.ProfileId}'.",
                SubjectId: entry.Manifest.ProfileId,
                RequiresConfirmation: true));
        }

        if (string.Equals(target.TargetKind, RuleProfileApplyTargetKinds.SessionLedger, StringComparison.Ordinal))
        {
            changes.Add(new RuleProfilePreviewItem(
                Kind: RuleProfilePreviewChangeKinds.SessionReplayRequired,
                Summary: "Session ledger targets may require replay or rebind after a profile-driven runtime change.",
                SubjectId: target.TargetId,
                RequiresConfirmation: true));
        }

        return changes.ToArray();
    }

    private static RuntimeInspectorWarning[] BuildWarnings(RuleProfileRegistryEntry entry)
    {
        List<RuntimeInspectorWarning> warnings = [];

        if (string.Equals(entry.Publication.Visibility, ArtifactVisibilityModes.LocalOnly, StringComparison.Ordinal))
        {
            warnings.Add(new RuntimeInspectorWarning(
                Kind: RuntimeInspectorWarningKinds.Trust,
                Severity: RuntimeInspectorWarningSeverityLevels.Info,
                Message: "Profile is derived from local-only RulePacks and is not portable to public catalogs without republishing.",
                SubjectId: entry.Manifest.ProfileId));
        }

        if (entry.Manifest.RulePacks.Count == 0)
        {
            warnings.Add(new RuntimeInspectorWarning(
                Kind: RuntimeInspectorWarningKinds.ProviderBinding,
                Severity: RuntimeInspectorWarningSeverityLevels.Info,
                Message: "Profile resolves to the built-in base runtime without additional RulePacks.",
                SubjectId: entry.Manifest.ProfileId));
        }

        return warnings.ToArray();
    }
}
