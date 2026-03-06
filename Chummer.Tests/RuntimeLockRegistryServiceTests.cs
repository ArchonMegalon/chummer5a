#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Linq;
using Chummer.Application.Content;
using Chummer.Contracts.Content;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Rulesets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class RuntimeLockRegistryServiceTests
{
    [TestMethod]
    public void Runtime_lock_registry_service_projects_profile_runtime_locks_into_catalog_entries()
    {
        ProfileBackedRuntimeLockRegistryService service = new(new RuleProfileRegistryServiceStub(
        [
            CreateProfile("official.sr5.core", "Official SR5 Core", ArtifactVisibilityModes.Public, "sha256:core"),
            CreateProfile("local.sr5.current-overlays", "SR5 Local Overlay Catalog", ArtifactVisibilityModes.LocalOnly, "sha256:overlay")
        ]));

        RuntimeLockRegistryPage page = service.List(OwnerScope.LocalSingleUser, RulesetDefaults.Sr5);

        Assert.HasCount(2, page.Entries);
        Assert.AreEqual(2, page.TotalCount);
        RuntimeLockRegistryEntry published = page.Entries.Single(entry => string.Equals(entry.LockId, "sha256:core", StringComparison.Ordinal));
        Assert.AreEqual(RuntimeLockCatalogKinds.Published, published.CatalogKind);
        RuntimeLockRegistryEntry derived = page.Entries.Single(entry => string.Equals(entry.LockId, "sha256:overlay", StringComparison.Ordinal));
        Assert.AreEqual(RuntimeLockCatalogKinds.Derived, derived.CatalogKind);
    }

    [TestMethod]
    public void Runtime_lock_registry_service_returns_null_for_unknown_lock()
    {
        ProfileBackedRuntimeLockRegistryService service = new(new RuleProfileRegistryServiceStub([]));

        RuntimeLockRegistryEntry? entry = service.Get(OwnerScope.LocalSingleUser, "missing-lock", RulesetDefaults.Sr5);

        Assert.IsNull(entry);
    }

    private static RuleProfileRegistryEntry CreateProfile(string profileId, string title, string visibility, string runtimeFingerprint)
    {
        return new RuleProfileRegistryEntry(
            new RuleProfileManifest(
                ProfileId: profileId,
                Title: title,
                Description: "Curated runtime.",
                RulesetId: RulesetDefaults.Sr5,
                Audience: RuleProfileAudienceKinds.General,
                CatalogKind: RuleProfileCatalogKinds.Official,
                RulePacks: [],
                DefaultToggles: [],
                RuntimeLock: new ResolvedRuntimeLock(
                    RulesetId: RulesetDefaults.Sr5,
                    ContentBundles:
                    [
                        new ContentBundleDescriptor(
                            BundleId: "official.sr5.base",
                            RulesetId: RulesetDefaults.Sr5,
                            Version: "schema-1",
                            Title: "SR5 Base",
                            Description: "Built-in base content.",
                            AssetPaths: ["data/", "lang/"])
                    ],
                    RulePacks: [],
                    ProviderBindings: new Dictionary<string, string>(StringComparer.Ordinal),
                    EngineApiVersion: "rulepack-v1",
                    RuntimeFingerprint: runtimeFingerprint),
                UpdateChannel: RuleProfileUpdateChannels.Stable),
            new RuleProfilePublicationMetadata(
                OwnerId: visibility == ArtifactVisibilityModes.Public ? "system" : "local-single-user",
                Visibility: visibility,
                PublicationStatus: RuleProfilePublicationStatuses.Published,
                Review: new RulePackReviewDecision(RulePackReviewStates.NotRequired),
                Shares: []));
    }

    private sealed class RuleProfileRegistryServiceStub : IRuleProfileRegistryService
    {
        private readonly IReadOnlyList<RuleProfileRegistryEntry> _entries;

        public RuleProfileRegistryServiceStub(IReadOnlyList<RuleProfileRegistryEntry> entries)
        {
            _entries = entries;
        }

        public IReadOnlyList<RuleProfileRegistryEntry> List(OwnerScope owner, string? rulesetId = null) => _entries;

        public RuleProfileRegistryEntry? Get(OwnerScope owner, string profileId, string? rulesetId = null)
        {
            return _entries.FirstOrDefault(entry => string.Equals(entry.Manifest.ProfileId, profileId, StringComparison.Ordinal));
        }
    }
}
