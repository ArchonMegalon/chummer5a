using System.Linq;
using Chummer.Contracts.Content;
using Chummer.Contracts.Owners;

namespace Chummer.Application.Content;

public sealed class ProfileBackedRuntimeLockRegistryService : IRuntimeLockRegistryService
{
    private readonly IRuleProfileRegistryService _ruleProfileRegistryService;
    private readonly IRuntimeLockStore _runtimeLockStore;

    public ProfileBackedRuntimeLockRegistryService(
        IRuleProfileRegistryService ruleProfileRegistryService,
        IRuntimeLockStore runtimeLockStore)
    {
        _ruleProfileRegistryService = ruleProfileRegistryService;
        _runtimeLockStore = runtimeLockStore;
    }

    public RuntimeLockRegistryPage List(OwnerScope owner, string? rulesetId = null)
    {
        Dictionary<string, RuntimeLockRegistryEntry> entries = _ruleProfileRegistryService.List(owner, rulesetId)
            .GroupBy(profile => profile.Manifest.RuntimeLock.RuntimeFingerprint, StringComparer.Ordinal)
            .Select(group => ToRegistryEntry(group.First()))
            .ToDictionary(entry => entry.LockId, StringComparer.Ordinal);

        foreach (RuntimeLockRegistryEntry persisted in _runtimeLockStore.List(owner, rulesetId).Entries)
        {
            entries[persisted.LockId] = persisted;
        }

        RuntimeLockRegistryEntry[] orderedEntries = entries.Values
            .OrderBy(entry => entry.Title, StringComparer.Ordinal)
            .ToArray();

        return new RuntimeLockRegistryPage(
            Entries: orderedEntries,
            TotalCount: orderedEntries.Length);
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
