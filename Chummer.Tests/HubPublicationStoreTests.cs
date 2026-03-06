#nullable enable annotations

using System;
using System.IO;
using Chummer.Contracts.Hub;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Rulesets;
using Chummer.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class HubPublicationStoreTests
{
    [TestMethod]
    public void File_hub_draft_store_persists_owner_scoped_draft_records()
    {
        string stateDirectory = CreateTempDirectory();

        try
        {
            FileHubDraftStore store = new(stateDirectory);
            HubDraftRecord record = new(
                DraftId: "draft-1",
                ProjectKind: HubCatalogItemKinds.RulePack,
                ProjectId: "campaign.shadowops",
                RulesetId: RulesetDefaults.Sr5,
                Title: "Campaign ShadowOps",
                OwnerId: "alice",
                State: HubPublicationStates.Draft,
                CreatedAtUtc: DateTimeOffset.Parse("2026-03-06T12:00:00+00:00"),
                UpdatedAtUtc: DateTimeOffset.Parse("2026-03-06T12:05:00+00:00"));

            store.Upsert(new OwnerScope("alice"), record);

            HubDraftRecord? reloaded = store.Get(new OwnerScope("alice"), HubCatalogItemKinds.RulePack, "campaign.shadowops", RulesetDefaults.Sr5);
            HubDraftRecord? hiddenFromBob = store.Get(new OwnerScope("bob"), HubCatalogItemKinds.RulePack, "campaign.shadowops", RulesetDefaults.Sr5);

            Assert.IsNotNull(reloaded);
            Assert.AreEqual("Campaign ShadowOps", reloaded.Title);
            Assert.AreEqual(HubPublicationStates.Draft, reloaded.State);
            Assert.IsNull(hiddenFromBob);
        }
        finally
        {
            Directory.Delete(stateDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void File_hub_moderation_case_store_persists_owner_scoped_case_records()
    {
        string stateDirectory = CreateTempDirectory();

        try
        {
            FileHubModerationCaseStore store = new(stateDirectory);
            HubModerationCaseRecord record = new(
                CaseId: "case-1",
                DraftId: "draft-1",
                ProjectKind: HubCatalogItemKinds.RuleProfile,
                ProjectId: "campaign.sr5.runtime",
                RulesetId: RulesetDefaults.Sr5,
                Title: "Campaign Runtime",
                OwnerId: "alice",
                State: HubModerationStates.PendingReview,
                CreatedAtUtc: DateTimeOffset.Parse("2026-03-06T12:10:00+00:00"),
                UpdatedAtUtc: DateTimeOffset.Parse("2026-03-06T12:11:00+00:00"),
                Summary: "Ready for review");

            store.Upsert(new OwnerScope("alice"), record);

            HubModerationCaseRecord? reloaded = store.Get(new OwnerScope("alice"), HubCatalogItemKinds.RuleProfile, "campaign.sr5.runtime", RulesetDefaults.Sr5);
            HubModerationCaseRecord? hiddenFromBob = store.Get(new OwnerScope("bob"), HubCatalogItemKinds.RuleProfile, "campaign.sr5.runtime", RulesetDefaults.Sr5);

            Assert.IsNotNull(reloaded);
            Assert.AreEqual("Campaign Runtime", reloaded.Title);
            Assert.AreEqual(HubModerationStates.PendingReview, reloaded.State);
            Assert.IsNull(hiddenFromBob);
        }
        finally
        {
            Directory.Delete(stateDirectory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"chummer-hub-publication-store-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
