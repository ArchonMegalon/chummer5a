using System.Linq;
using Chummer.Contracts.Content;
using Chummer.Contracts.Owners;

namespace Chummer.Application.Content;

public sealed class DefaultRuntimeInspectorService : IRuntimeInspectorService
{
    private readonly IRuleProfileRegistryService _ruleProfileRegistryService;
    private readonly IRulePackRegistryService _rulePackRegistryService;

    public DefaultRuntimeInspectorService(
        IRuleProfileRegistryService ruleProfileRegistryService,
        IRulePackRegistryService rulePackRegistryService)
    {
        _ruleProfileRegistryService = ruleProfileRegistryService;
        _rulePackRegistryService = rulePackRegistryService;
    }

    public RuntimeInspectorProjection? GetProfileProjection(OwnerScope owner, string profileId, string? rulesetId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);

        RuleProfileRegistryEntry? profile = _ruleProfileRegistryService.Get(owner, profileId, rulesetId);
        if (profile is null)
        {
            return null;
        }

        IReadOnlyDictionary<string, RulePackRegistryEntry> registryEntries = _rulePackRegistryService.List(owner, profile.Manifest.RulesetId)
            .ToDictionary(entry => entry.Manifest.PackId, StringComparer.Ordinal);

        RuntimeInspectorRulePackEntry[] resolvedRulePacks = profile.Manifest.RulePacks
            .Select(selection => ToResolvedRulePackEntry(selection, registryEntries.GetValueOrDefault(selection.RulePack.Id)))
            .ToArray();
        RuntimeInspectorProviderBinding[] providerBindings = profile.Manifest.RuntimeLock.ProviderBindings
            .Select(binding => new RuntimeInspectorProviderBinding(
                CapabilityId: binding.Key,
                ProviderId: binding.Value,
                PackId: TryResolvePackId(binding.Value, registryEntries.Keys),
                SourceAssetPath: null,
                SessionSafe: false))
            .ToArray();
        RuntimeLockCompatibilityDiagnostic[] compatibilityDiagnostics = BuildCompatibilityDiagnostics(profile, registryEntries);
        RuntimeInspectorWarning[] warnings = BuildWarnings(profile, resolvedRulePacks, compatibilityDiagnostics);
        RuntimeMigrationPreviewItem[] migrationPreview = BuildMigrationPreview(profile, resolvedRulePacks);

        return new RuntimeInspectorProjection(
            TargetKind: RuntimeInspectorTargetKinds.RuntimeLock,
            TargetId: profile.Manifest.ProfileId,
            RuntimeLock: profile.Manifest.RuntimeLock,
            Install: NormalizeInstall(profile.Install, profile.Manifest.RuntimeLock.RuntimeFingerprint),
            ResolvedRulePacks: resolvedRulePacks,
            ProviderBindings: providerBindings,
            CompatibilityDiagnostics: compatibilityDiagnostics,
            Warnings: warnings,
            MigrationPreview: migrationPreview,
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            ProfileSourceKind: profile.SourceKind);
    }

    private static RuntimeInspectorRulePackEntry ToResolvedRulePackEntry(
        RuleProfilePackSelection selection,
        RulePackRegistryEntry? registryEntry)
    {
        return new RuntimeInspectorRulePackEntry(
            RulePack: selection.RulePack,
            Title: registryEntry?.Manifest.Title ?? selection.RulePack.Id,
            Visibility: registryEntry?.Publication.Visibility ?? ArtifactVisibilityModes.LocalOnly,
            TrustTier: registryEntry?.Manifest.TrustTier ?? ArtifactTrustTiers.LocalOnly,
            CapabilityIds: registryEntry?.Manifest.Capabilities.Select(capability => capability.CapabilityId).ToArray() ?? [],
            Enabled: selection.EnabledByDefault,
            SourceKind: registryEntry?.SourceKind ?? RegistryEntrySourceKinds.PersistedManifest);
    }

    private static RuntimeLockCompatibilityDiagnostic[] BuildCompatibilityDiagnostics(
        RuleProfileRegistryEntry profile,
        IReadOnlyDictionary<string, RulePackRegistryEntry> registryEntries)
    {
        List<RuntimeLockCompatibilityDiagnostic> diagnostics = [];

        foreach (RuleProfilePackSelection selection in profile.Manifest.RulePacks)
        {
            if (!registryEntries.ContainsKey(selection.RulePack.Id))
            {
                diagnostics.Add(new RuntimeLockCompatibilityDiagnostic(
                    State: RuntimeLockCompatibilityStates.MissingPack,
                    Message: $"Required RulePack '{selection.RulePack.Id}' is not present in the current registry.",
                    RequiredRulesetId: profile.Manifest.RulesetId,
                    RequiredRuntimeFingerprint: profile.Manifest.RuntimeLock.RuntimeFingerprint));
            }
        }

        if (diagnostics.Count == 0)
        {
            diagnostics.Add(new RuntimeLockCompatibilityDiagnostic(
                State: RuntimeLockCompatibilityStates.Compatible,
                Message: "Runtime lock resolves against the current RuleProfile and RulePack catalog.",
                RequiredRulesetId: profile.Manifest.RulesetId,
                RequiredRuntimeFingerprint: profile.Manifest.RuntimeLock.RuntimeFingerprint));
        }

        return diagnostics.ToArray();
    }

    private static RuntimeInspectorWarning[] BuildWarnings(
        RuleProfileRegistryEntry profile,
        IReadOnlyList<RuntimeInspectorRulePackEntry> resolvedRulePacks,
        IReadOnlyList<RuntimeLockCompatibilityDiagnostic> compatibilityDiagnostics)
    {
        List<RuntimeInspectorWarning> warnings = [];

        if (string.Equals(profile.Publication.Visibility, ArtifactVisibilityModes.LocalOnly, StringComparison.Ordinal))
        {
            warnings.Add(new RuntimeInspectorWarning(
                Kind: RuntimeInspectorWarningKinds.Trust,
                Severity: RuntimeInspectorWarningSeverityLevels.Info,
                Message: "Profile is local-only and should be republished before public distribution.",
                SubjectId: profile.Manifest.ProfileId));
        }

        if (compatibilityDiagnostics.Any(diagnostic => string.Equals(diagnostic.State, RuntimeLockCompatibilityStates.MissingPack, StringComparison.Ordinal)))
        {
            warnings.Add(new RuntimeInspectorWarning(
                Kind: RuntimeInspectorWarningKinds.Compatibility,
                Severity: RuntimeInspectorWarningSeverityLevels.Warning,
                Message: "One or more RulePacks referenced by the profile are missing from the current catalog.",
                SubjectId: profile.Manifest.ProfileId));
        }

        if (resolvedRulePacks.Count == 0)
        {
            warnings.Add(new RuntimeInspectorWarning(
                Kind: RuntimeInspectorWarningKinds.ProviderBinding,
                Severity: RuntimeInspectorWarningSeverityLevels.Info,
                Message: "Runtime resolves to built-in base content without additional RulePacks.",
                SubjectId: profile.Manifest.ProfileId));
        }

        return warnings.ToArray();
    }

    private static RuntimeMigrationPreviewItem[] BuildMigrationPreview(
        RuleProfileRegistryEntry profile,
        IReadOnlyList<RuntimeInspectorRulePackEntry> resolvedRulePacks)
    {
        List<RuntimeMigrationPreviewItem> preview = resolvedRulePacks
            .Select(rulePack => new RuntimeMigrationPreviewItem(
                Kind: RuntimeMigrationPreviewChangeKinds.RulePackAdded,
                Summary: $"Profile applies RulePack '{rulePack.RulePack.Id}@{rulePack.RulePack.Version}'.",
                SubjectId: rulePack.RulePack.Id,
                AfterValue: rulePack.RulePack.Version,
                RequiresRebind: false))
            .ToList();

        if (preview.Count == 0)
        {
            preview.Add(new RuntimeMigrationPreviewItem(
                Kind: RuntimeMigrationPreviewChangeKinds.ContentBundleUpdated,
                Summary: $"Profile '{profile.Manifest.ProfileId}' pins the built-in runtime fingerprint '{profile.Manifest.RuntimeLock.RuntimeFingerprint}'.",
                SubjectId: profile.Manifest.RuntimeLock.RuntimeFingerprint,
                AfterValue: profile.Manifest.RuntimeLock.RuntimeFingerprint,
                RequiresRebind: false));
        }

        return preview.ToArray();
    }

    private static string? TryResolvePackId(string providerId, IEnumerable<string> packIds)
    {
        foreach (string packId in packIds)
        {
            if (providerId.StartsWith($"{packId}/", StringComparison.Ordinal))
            {
                return packId;
            }
        }

        return null;
    }

    private static ArtifactInstallState NormalizeInstall(ArtifactInstallState install, string runtimeFingerprint)
    {
        return string.IsNullOrWhiteSpace(install.RuntimeFingerprint)
            ? install with { RuntimeFingerprint = runtimeFingerprint }
            : install;
    }
}
