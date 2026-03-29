using Chummer.Contracts.Content;

namespace Chummer.Application.Content;

internal static class RuntimeInspectorPromotionNarrator
{
    public static ArtifactInstallState NormalizeInstall(ArtifactInstallState install, string runtimeFingerprint)
    {
        return string.IsNullOrWhiteSpace(install.RuntimeFingerprint)
            ? install with { RuntimeFingerprint = runtimeFingerprint }
            : install;
    }

    public static RuntimeInspectorPromotionProjection BuildPromotion(RuleProfileRegistryEntry profile, ArtifactInstallState install)
    {
        string promotionSummary = profile.Manifest.UpdateChannel switch
        {
            RuleProfileUpdateChannels.Stable => $"Stable rule environment is {profile.Publication.PublicationStatus} with {profile.Publication.Visibility} visibility and ready for governed reuse.",
            RuleProfileUpdateChannels.Preview => $"Preview rule environment is {profile.Publication.PublicationStatus} with {profile.Publication.Visibility} visibility and stays on the sandbox rail until promotion is explicit.",
            RuleProfileUpdateChannels.CampaignPinned => $"Campaign-pinned rule environment is {profile.Publication.PublicationStatus} with {profile.Publication.Visibility} visibility and stays on the campaign-approved rail until broader promotion is chosen.",
            _ => $"Rule environment is {profile.Publication.PublicationStatus} on update channel {profile.Manifest.UpdateChannel}."
        };

        string rollbackSummary = string.IsNullOrWhiteSpace(install.InstalledTargetKind)
            ? "No install target is pinned yet; rollback still needs the first governed pin before promotion."
            : $"Rollback can re-pin {install.RuntimeFingerprint} on {FormatInstallTarget(install)} while the next promotion is reviewed.";

        string lineageSummary = string.Equals(profile.SourceKind, RegistryEntrySourceKinds.BuiltInCoreProfile, StringComparison.Ordinal)
            ? "Built-in core profile remains the baseline lineage anchor for this runtime."
            : $"{profile.SourceKind} profile compiles on top of the governed runtime lock instead of forking a local shadow rule environment.";

        return new RuntimeInspectorPromotionProjection(
            PublicationStatus: profile.Publication.PublicationStatus,
            Visibility: profile.Publication.Visibility,
            UpdateChannel: profile.Manifest.UpdateChannel,
            PromotionSummary: promotionSummary,
            RollbackSummary: rollbackSummary,
            LineageSummary: lineageSummary,
            PublishedAtUtc: profile.Publication.PublishedAtUtc);
    }

    private static string FormatInstallTarget(ArtifactInstallState install)
    {
        if (string.IsNullOrWhiteSpace(install.InstalledTargetKind))
        {
            return "(none)";
        }

        return string.IsNullOrWhiteSpace(install.InstalledTargetId)
            ? install.InstalledTargetKind
            : $"{install.InstalledTargetKind}:{install.InstalledTargetId}";
    }
}
