#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Linq;
using Chummer.Application.Hub;
using Chummer.Contracts.Hub;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Rulesets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class HubPublicationServiceTests
{
    [TestMethod]
    public void Default_publication_service_persists_and_lists_owner_drafts()
    {
        InMemoryHubDraftStore draftStore = new();
        DefaultHubPublicationService service = new(draftStore, new InMemoryHubModerationCaseStore());

        HubPublicationResult<HubPublishDraftReceipt> created = service.CreateDraft(
            new OwnerScope("alice"),
            new HubPublishDraftRequest(
                ProjectKind: HubCatalogItemKinds.RulePack,
                ProjectId: "campaign.shadowops",
                RulesetId: RulesetDefaults.Sr5,
                Title: "Campaign ShadowOps"));
        HubPublicationResult<HubPublishDraftList> listed = service.ListDrafts(new OwnerScope("alice"), HubCatalogItemKinds.RulePack, RulesetDefaults.Sr5);
        HubPublicationResult<HubPublishDraftList> hiddenFromBob = service.ListDrafts(new OwnerScope("bob"), HubCatalogItemKinds.RulePack, RulesetDefaults.Sr5);

        Assert.IsTrue(created.IsImplemented);
        Assert.IsNotNull(created.Payload);
        Assert.AreEqual(HubPublicationStates.Draft, created.Payload.State);
        Assert.HasCount(1, listed.Payload!.Items);
        Assert.AreEqual("campaign.shadowops", listed.Payload.Items[0].ProjectId);
        Assert.IsEmpty(hiddenFromBob.Payload!.Items);
    }

    [TestMethod]
    public void Default_publication_service_submit_updates_draft_and_creates_owner_queue_entry()
    {
        InMemoryHubDraftStore draftStore = new();
        InMemoryHubModerationCaseStore moderationCaseStore = new();
        DefaultHubPublicationService publicationService = new(draftStore, moderationCaseStore);
        DefaultHubModerationService moderationService = new(moderationCaseStore);
        OwnerScope owner = new("alice");

        publicationService.CreateDraft(
            owner,
            new HubPublishDraftRequest(
                ProjectKind: HubCatalogItemKinds.RuleProfile,
                ProjectId: "campaign.sr5.runtime",
                RulesetId: RulesetDefaults.Sr5,
                Title: "Campaign Runtime"));

        HubPublicationResult<HubProjectSubmissionReceipt> submission = publicationService.SubmitForReview(
            owner,
            HubCatalogItemKinds.RuleProfile,
            "campaign.sr5.runtime",
            RulesetDefaults.Sr5,
            new HubSubmitProjectRequest("ready for review"));
        HubPublicationResult<HubModerationQueue> queue = moderationService.ListQueue(owner, HubModerationStates.PendingReview);

        Assert.IsTrue(submission.IsImplemented);
        Assert.IsNotNull(submission.Payload);
        Assert.AreEqual(HubPublicationStates.Submitted, submission.Payload.State);
        Assert.AreEqual(HubModerationStates.PendingReview, submission.Payload.ReviewState);
        Assert.HasCount(1, queue.Payload!.Items);
        Assert.AreEqual("campaign.sr5.runtime", queue.Payload.Items[0].ProjectId);
        Assert.AreEqual("Campaign Runtime", queue.Payload.Items[0].Title);
    }

    private sealed class InMemoryHubDraftStore : IHubDraftStore
    {
        private readonly List<HubDraftRecord> _records = [];

        public IReadOnlyList<HubDraftRecord> List(OwnerScope owner, string? kind = null, string? rulesetId = null, string? state = null)
        {
            return _records
                .Where(record => string.Equals(record.OwnerId, owner.NormalizedValue, StringComparison.Ordinal))
                .Where(record => kind is null || string.Equals(record.ProjectKind, kind, StringComparison.Ordinal))
                .Where(record => rulesetId is null || string.Equals(record.RulesetId, rulesetId, StringComparison.Ordinal))
                .Where(record => state is null || string.Equals(record.State, state, StringComparison.Ordinal))
                .ToArray();
        }

        public HubDraftRecord? Get(OwnerScope owner, string kind, string projectId, string rulesetId)
        {
            return List(owner, kind, rulesetId).FirstOrDefault(record => string.Equals(record.ProjectId, projectId, StringComparison.Ordinal));
        }

        public HubDraftRecord Upsert(OwnerScope owner, HubDraftRecord record)
        {
            int existingIndex = _records.FindIndex(current =>
                string.Equals(current.OwnerId, owner.NormalizedValue, StringComparison.Ordinal)
                && string.Equals(current.ProjectKind, record.ProjectKind, StringComparison.Ordinal)
                && string.Equals(current.ProjectId, record.ProjectId, StringComparison.Ordinal)
                && string.Equals(current.RulesetId, record.RulesetId, StringComparison.Ordinal));
            HubDraftRecord normalizedRecord = record with { OwnerId = owner.NormalizedValue };
            if (existingIndex >= 0)
            {
                _records[existingIndex] = normalizedRecord;
            }
            else
            {
                _records.Add(normalizedRecord);
            }

            return normalizedRecord;
        }
    }

    private sealed class InMemoryHubModerationCaseStore : IHubModerationCaseStore
    {
        private readonly List<HubModerationCaseRecord> _records = [];

        public IReadOnlyList<HubModerationCaseRecord> List(OwnerScope owner, string? kind = null, string? rulesetId = null, string? state = null)
        {
            return _records
                .Where(record => string.Equals(record.OwnerId, owner.NormalizedValue, StringComparison.Ordinal))
                .Where(record => kind is null || string.Equals(record.ProjectKind, kind, StringComparison.Ordinal))
                .Where(record => rulesetId is null || string.Equals(record.RulesetId, rulesetId, StringComparison.Ordinal))
                .Where(record => state is null || string.Equals(record.State, state, StringComparison.Ordinal))
                .ToArray();
        }

        public HubModerationCaseRecord? Get(OwnerScope owner, string kind, string projectId, string rulesetId)
        {
            return List(owner, kind, rulesetId).FirstOrDefault(record => string.Equals(record.ProjectId, projectId, StringComparison.Ordinal));
        }

        public HubModerationCaseRecord Upsert(OwnerScope owner, HubModerationCaseRecord record)
        {
            int existingIndex = _records.FindIndex(current =>
                string.Equals(current.OwnerId, owner.NormalizedValue, StringComparison.Ordinal)
                && string.Equals(current.ProjectKind, record.ProjectKind, StringComparison.Ordinal)
                && string.Equals(current.ProjectId, record.ProjectId, StringComparison.Ordinal)
                && string.Equals(current.RulesetId, record.RulesetId, StringComparison.Ordinal));
            HubModerationCaseRecord normalizedRecord = record with { OwnerId = owner.NormalizedValue };
            if (existingIndex >= 0)
            {
                _records[existingIndex] = normalizedRecord;
            }
            else
            {
                _records.Add(normalizedRecord);
            }

            return normalizedRecord;
        }
    }
}
