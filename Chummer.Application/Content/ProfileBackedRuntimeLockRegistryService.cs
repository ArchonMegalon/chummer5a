using System.Linq;
using Chummer.Contracts.Content;
using Chummer.Contracts.Owners;

namespace Chummer.Application.Content;

public sealed class ProfileBackedRuntimeLockRegistryService : IRuntimeLockRegistryService
{
    private readonly IRuleProfileRegistryService _ruleProfileRegistryService;

    public ProfileBackedRuntimeLockRegistryService(IRuleProfileRegistryService ruleProfileRegistryService)
    {
        _ruleProfileRegistryService = ruleProfileRegistryService;
    }

    public RuntimeLockRegistryPage List(OwnerScope owner, string? rulesetId = null)
    {
        RuntimeLockRegistryEntry[] entries = _ruleProfileRegistryService.List(owner, rulesetId)
            .GroupBy(profile => profile.Manifest.RuntimeLock.RuntimeFingerprint, StringComparer.Ordinal)
            .Select(group => ToRegistryEntry(group.First()))
            .OrderBy(entry => entry.Title, StringComparer.Ordinal)
            .ToArray();

        return new RuntimeLockRegistryPage(
            Entries: entries,
            TotalCount: entries.Length);
    }

    public RuntimeLockRegistryEntry? Get(OwnerScope owner, string lockId, string? rulesetId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lockId);

        return List(owner, rulesetId).Entries
            .FirstOrDefault(entry => string.Equals(entry.LockId, lockId, StringComparison.Ordinal));
    }

    private static RuntimeLockRegistryEntry ToRegistryEntry(RuleProfileRegistryEntry profile)
    {
        string ownerId = string.IsNullOrWhiteSpace(profile.Publication.OwnerId)
            ? OwnerScope.LocalSingleUser.NormalizedValue
            : profile.Publication.OwnerId;

        return new RuntimeLockRegistryEntry(
            LockId: profile.Manifest.RuntimeLock.RuntimeFingerprint,
            Owner: new OwnerScope(ownerId),
            Title: $"{profile.Manifest.Title} Runtime Lock",
            Visibility: profile.Publication.Visibility,
            CatalogKind: ResolveCatalogKind(profile),
            RuntimeLock: profile.Manifest.RuntimeLock,
            UpdatedAtUtc: profile.Publication.PublishedAtUtc ?? DateTimeOffset.UtcNow,
            Description: profile.Manifest.Description);
    }

    private static string ResolveCatalogKind(RuleProfileRegistryEntry profile)
    {
        return string.Equals(profile.Publication.Visibility, ArtifactVisibilityModes.Public, StringComparison.Ordinal)
            ? RuntimeLockCatalogKinds.Published
            : RuntimeLockCatalogKinds.Derived;
    }
}
