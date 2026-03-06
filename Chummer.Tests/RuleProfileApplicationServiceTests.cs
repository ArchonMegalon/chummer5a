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
public class RuleProfileApplicationServiceTests
{
    [TestMethod]
    public void Default_application_service_previews_runtime_lock_and_changes_for_registered_profile()
    {
        DefaultRuleProfileApplicationService service = new(new RuleProfileRegistryServiceStub(CreateProfileEntry(includeRulePack: true)));

        RuleProfilePreviewReceipt? preview = service.Preview(
            OwnerScope.LocalSingleUser,
            "official.sr5.core",
            new RuleProfileApplyTarget(RuleProfileApplyTargetKinds.Workspace, "workspace-1"));

        Assert.IsNotNull(preview);
        Assert.AreEqual("official.sr5.core", preview.ProfileId);
        Assert.AreEqual("workspace-1", preview.Target.TargetId);
        Assert.AreEqual("runtime-lock-sha256", preview.RuntimeLock.RuntimeFingerprint);
        Assert.IsTrue(preview.RequiresConfirmation);
        Assert.IsTrue(preview.Changes.Any(change => string.Equals(change.Kind, RuleProfilePreviewChangeKinds.RulePackSelectionChanged, StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Default_application_service_returns_deferred_apply_receipt_until_binding_flows_exist()
    {
        DefaultRuleProfileApplicationService service = new(new RuleProfileRegistryServiceStub(CreateProfileEntry(includeRulePack: false)));

        RuleProfileApplyReceipt? receipt = service.Apply(
            OwnerScope.LocalSingleUser,
            "official.sr5.core",
            new RuleProfileApplyTarget(RuleProfileApplyTargetKinds.Character, "character-1"));

        Assert.IsNotNull(receipt);
        Assert.AreEqual(RuleProfileApplyOutcomes.Deferred, receipt.Outcome);
        Assert.AreEqual("ruleprofile_apply_not_implemented", receipt.DeferredReason);
        Assert.IsNotNull(receipt.Preview);
        Assert.AreEqual("character-1", receipt.Target.TargetId);
    }

    private static RuleProfileRegistryEntry CreateProfileEntry(bool includeRulePack)
    {
        List<RuleProfilePackSelection> rulePacks = [];
        if (includeRulePack)
        {
            rulePacks.Add(new RuleProfilePackSelection(
                new ArtifactVersionReference("house-rules", "1.0.0"),
                Required: true,
                EnabledByDefault: true));
        }

        return new RuleProfileRegistryEntry(
            new RuleProfileManifest(
                ProfileId: "official.sr5.core",
                Title: "Official SR5 Core",
                Description: "Curated runtime.",
                RulesetId: RulesetDefaults.Sr5,
                Audience: RuleProfileAudienceKinds.General,
                CatalogKind: RuleProfileCatalogKinds.Official,
                RulePacks: rulePacks,
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
                    RulePacks: includeRulePack ? [new ArtifactVersionReference("house-rules", "1.0.0")] : [],
                    ProviderBindings: new Dictionary<string, string>(StringComparer.Ordinal),
                    EngineApiVersion: "rulepack-v1",
                    RuntimeFingerprint: "runtime-lock-sha256"),
                UpdateChannel: RuleProfileUpdateChannels.Stable),
            new RuleProfilePublicationMetadata(
                OwnerId: "system",
                Visibility: includeRulePack ? ArtifactVisibilityModes.LocalOnly : ArtifactVisibilityModes.Public,
                PublicationStatus: RuleProfilePublicationStatuses.Published,
                Review: new RulePackReviewDecision(RulePackReviewStates.NotRequired),
                Shares: []));
    }

    private sealed class RuleProfileRegistryServiceStub : IRuleProfileRegistryService
    {
        private readonly RuleProfileRegistryEntry _entry;

        public RuleProfileRegistryServiceStub(RuleProfileRegistryEntry entry)
        {
            _entry = entry;
        }

        public IReadOnlyList<RuleProfileRegistryEntry> List(OwnerScope owner, string? rulesetId = null) => [_entry];

        public RuleProfileRegistryEntry? Get(OwnerScope owner, string profileId, string? rulesetId = null)
        {
            return string.Equals(profileId, _entry.Manifest.ProfileId, StringComparison.Ordinal)
                ? _entry
                : null;
        }
    }
}
