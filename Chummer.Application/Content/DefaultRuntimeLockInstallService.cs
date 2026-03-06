using Chummer.Contracts.Content;
using Chummer.Contracts.Owners;

namespace Chummer.Application.Content;

public sealed class DefaultRuntimeLockInstallService : IRuntimeLockInstallService
{
    private readonly IRuntimeLockInstallHistoryStore _installHistoryStore;
    private readonly IRuntimeLockRegistryService _runtimeLockRegistryService;
    private readonly IRuntimeLockStore _runtimeLockStore;

    public DefaultRuntimeLockInstallService(
        IRuntimeLockRegistryService runtimeLockRegistryService,
        IRuntimeLockStore runtimeLockStore,
        IRuntimeLockInstallHistoryStore installHistoryStore)
    {
        _runtimeLockRegistryService = runtimeLockRegistryService;
        _runtimeLockStore = runtimeLockStore;
        _installHistoryStore = installHistoryStore;
    }

    public RuntimeLockInstallPreviewReceipt? Preview(OwnerScope owner, string lockId, RuleProfileApplyTarget target, string? rulesetId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lockId);
        ArgumentException.ThrowIfNullOrWhiteSpace(target.TargetKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(target.TargetId);

        RuntimeLockRegistryEntry? entry = _runtimeLockRegistryService.Get(owner, lockId, rulesetId);
        if (entry is null)
        {
            return null;
        }

        List<RuntimeLockInstallPreviewItem> changes =
        [
            new(
                Kind: RuntimeLockInstallPreviewChangeKinds.RuntimeLockPinned,
                Summary: $"Pin runtime '{entry.RuntimeLock.RuntimeFingerprint}' to {target.TargetKind} '{target.TargetId}'.",
                SubjectId: entry.LockId)
        ];
        List<RuntimeInspectorWarning> warnings = BuildWarnings(entry);
        if (string.Equals(target.TargetKind, RuleProfileApplyTargetKinds.SessionLedger, StringComparison.Ordinal))
        {
            changes.Add(new RuntimeLockInstallPreviewItem(
                Kind: RuntimeLockInstallPreviewChangeKinds.SessionReplayRequired,
                Summary: "Session ledger targets may require replay or rebind after a runtime-lock change.",
                SubjectId: target.TargetId,
                RequiresConfirmation: true));
        }

        bool requiresConfirmation = changes.Any(change => change.RequiresConfirmation);
        return new RuntimeLockInstallPreviewReceipt(
            LockId: entry.LockId,
            Target: target,
            RuntimeLock: entry.RuntimeLock,
            Changes: changes,
            Warnings: warnings,
            RequiresConfirmation: requiresConfirmation);
    }

    public RuntimeLockInstallReceipt? Apply(OwnerScope owner, string lockId, RuleProfileApplyTarget target, string? rulesetId = null)
    {
        RuntimeLockInstallPreviewReceipt? preview = Preview(owner, lockId, target, rulesetId);
        if (preview is null)
        {
            return null;
        }

        RuntimeLockRegistryEntry? entry = _runtimeLockRegistryService.Get(owner, lockId, rulesetId);
        if (entry is null)
        {
            return null;
        }

        ArtifactInstallState current = entry.Install;
        if (string.Equals(current.State, ArtifactInstallStates.Pinned, StringComparison.Ordinal)
            && string.Equals(current.InstalledTargetKind, target.TargetKind, StringComparison.Ordinal)
            && string.Equals(current.InstalledTargetId, target.TargetId, StringComparison.Ordinal))
        {
            return new RuntimeLockInstallReceipt(
                TargetKind: target.TargetKind,
                TargetId: target.TargetId,
                Outcome: RuntimeLockInstallOutcomes.Unchanged,
                RuntimeLock: preview.RuntimeLock,
                InstalledAtUtc: current.InstalledAtUtc ?? DateTimeOffset.UtcNow,
                RebindNotices: [],
                RequiresSessionReplay: false);
        }

        DateTimeOffset appliedAtUtc = DateTimeOffset.UtcNow;
        ArtifactInstallState install = new(
            State: ArtifactInstallStates.Pinned,
            InstalledAtUtc: appliedAtUtc,
            InstalledTargetKind: target.TargetKind,
            InstalledTargetId: target.TargetId,
            RuntimeFingerprint: preview.RuntimeLock.RuntimeFingerprint);
        RuntimeLockRegistryEntry persistedEntry = _runtimeLockStore.Upsert(
            owner,
            entry with
            {
                Owner = owner,
                Visibility = RuntimeLockCatalogKinds.Saved == entry.CatalogKind
                    ? entry.Visibility
                    : ArtifactVisibilityModes.LocalOnly,
                CatalogKind = RuntimeLockCatalogKinds.Saved,
                UpdatedAtUtc = appliedAtUtc,
                Install = install
            });
        _installHistoryStore.Append(
            owner,
            new RuntimeLockInstallHistoryRecord(
                LockId: persistedEntry.LockId,
                RulesetId: persistedEntry.RuntimeLock.RulesetId,
                Entry: new ArtifactInstallHistoryEntry(
                    Operation: ArtifactInstallHistoryOperations.Pin,
                    Install: persistedEntry.Install,
                    AppliedAtUtc: appliedAtUtc)));

        return new RuntimeLockInstallReceipt(
            TargetKind: target.TargetKind,
            TargetId: target.TargetId,
            Outcome: string.Equals(current.State, ArtifactInstallStates.Available, StringComparison.Ordinal)
                ? RuntimeLockInstallOutcomes.Installed
                : RuntimeLockInstallOutcomes.Updated,
            RuntimeLock: preview.RuntimeLock,
            InstalledAtUtc: persistedEntry.Install.InstalledAtUtc ?? appliedAtUtc,
            RebindNotices: [],
            RequiresSessionReplay: string.Equals(target.TargetKind, RuleProfileApplyTargetKinds.SessionLedger, StringComparison.Ordinal));
    }

    private static List<RuntimeInspectorWarning> BuildWarnings(RuntimeLockRegistryEntry entry)
    {
        List<RuntimeInspectorWarning> warnings = [];
        if (entry.RuntimeLock.RulePacks.Count == 0)
        {
            warnings.Add(new RuntimeInspectorWarning(
                Kind: RuntimeInspectorWarningKinds.ProviderBinding,
                Severity: RuntimeInspectorWarningSeverityLevels.Info,
                Message: "Runtime lock resolves to built-in content without additional RulePacks.",
                SubjectId: entry.LockId));
        }

        if (string.Equals(entry.Visibility, ArtifactVisibilityModes.LocalOnly, StringComparison.Ordinal))
        {
            warnings.Add(new RuntimeInspectorWarning(
                Kind: RuntimeInspectorWarningKinds.Trust,
                Severity: RuntimeInspectorWarningSeverityLevels.Info,
                Message: "Runtime lock is local-only and will need export or publication before other owners can reuse it.",
                SubjectId: entry.LockId));
        }

        return warnings;
    }
}
