using System.Linq;
using Chummer.Contracts.Content;
using Chummer.Contracts.Owners;

namespace Chummer.Application.Content;

public sealed class DefaultRuleProfileApplicationService : IRuleProfileApplicationService
{
    private readonly IRuleProfileRegistryService _ruleProfileRegistryService;

    public DefaultRuleProfileApplicationService(IRuleProfileRegistryService ruleProfileRegistryService)
    {
        _ruleProfileRegistryService = ruleProfileRegistryService;
    }

    public RuleProfilePreviewReceipt? Preview(OwnerScope owner, string profileId, RuleProfileApplyTarget target, string? rulesetId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(target.TargetKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(target.TargetId);

        RuleProfileRegistryEntry? entry = _ruleProfileRegistryService.Get(owner, profileId, rulesetId);
        if (entry is null)
        {
            return null;
        }

        RuleProfilePreviewItem[] changes = BuildPreviewChanges(entry, target);
        RuntimeInspectorWarning[] warnings = BuildWarnings(entry);

        return new RuleProfilePreviewReceipt(
            ProfileId: entry.Manifest.ProfileId,
            Target: target,
            RuntimeLock: entry.Manifest.RuntimeLock,
            Changes: changes,
            Warnings: warnings,
            RequiresConfirmation: changes.Any(change => change.RequiresConfirmation));
    }

    public RuleProfileApplyReceipt? Apply(OwnerScope owner, string profileId, RuleProfileApplyTarget target, string? rulesetId = null)
    {
        RuleProfilePreviewReceipt? preview = Preview(owner, profileId, target, rulesetId);
        if (preview is null)
        {
            return null;
        }

        return new RuleProfileApplyReceipt(
            ProfileId: preview.ProfileId,
            Target: preview.Target,
            Outcome: RuleProfileApplyOutcomes.Deferred,
            Preview: preview,
            DeferredReason: "ruleprofile_apply_not_implemented");
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
