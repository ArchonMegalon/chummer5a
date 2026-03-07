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
public sealed class HubReviewServiceTests
{
    [TestMethod]
    public void Default_review_service_upserts_and_lists_owner_reviews()
    {
        InMemoryHubReviewStore store = new();
        DefaultHubReviewService service = new(store);
        OwnerScope owner = new("alice");

        HubReviewReceipt review = service.UpsertReview(
            owner,
            HubCatalogItemKinds.RulePack,
            "campaign.shadowops",
            new HubUpsertReviewRequest(
                RulesetId: RulesetDefaults.Sr5,
                RecommendationState: HubRecommendationStates.Recommended,
                Stars: 5,
                ReviewText: "Great pack",
                UsedAtTable: true)).Payload!;
        HubReviewCatalog catalog = service.ListReviews(owner, HubCatalogItemKinds.RulePack, "campaign.shadowops", RulesetDefaults.Sr5).Payload!;

        Assert.AreEqual(HubRecommendationStates.Recommended, review.RecommendationState);
        Assert.AreEqual(5, review.Stars);
        Assert.AreEqual("Great pack", review.ReviewText);
        Assert.IsTrue(review.UsedAtTable);
        Assert.HasCount(1, catalog.Items);
        Assert.AreEqual(review.ReviewId, catalog.Items[0].ReviewId);
    }

    [TestMethod]
    public void Default_review_service_is_owner_scoped()
    {
        InMemoryHubReviewStore store = new();
        DefaultHubReviewService service = new(store);
        service.UpsertReview(
            new OwnerScope("alice"),
            HubCatalogItemKinds.RuleProfile,
            "campaign.sr5.runtime",
            new HubUpsertReviewRequest(RulesetDefaults.Sr5, HubRecommendationStates.Neutral));

        HubReviewCatalog bobCatalog = service.ListReviews(new OwnerScope("bob")).Payload!;

        Assert.IsEmpty(bobCatalog.Items);
    }

    private sealed class InMemoryHubReviewStore : IHubReviewStore
    {
        private readonly List<HubReviewRecord> _records = [];

        public IReadOnlyList<HubReviewRecord> List(OwnerScope owner, string? kind = null, string? itemId = null, string? rulesetId = null)
        {
            return _records
                .Where(record => string.Equals(record.OwnerId, owner.NormalizedValue, StringComparison.Ordinal))
                .Where(record => kind is null || string.Equals(record.ProjectKind, kind, StringComparison.Ordinal))
                .Where(record => itemId is null || string.Equals(record.ProjectId, itemId, StringComparison.Ordinal))
                .Where(record => rulesetId is null || string.Equals(record.RulesetId, rulesetId, StringComparison.Ordinal))
                .ToArray();
        }

        public HubReviewRecord? Get(OwnerScope owner, string kind, string itemId, string rulesetId)
        {
            return _records.Find(record =>
                string.Equals(record.OwnerId, owner.NormalizedValue, StringComparison.Ordinal)
                && string.Equals(record.ProjectKind, kind, StringComparison.Ordinal)
                && string.Equals(record.ProjectId, itemId, StringComparison.Ordinal)
                && string.Equals(record.RulesetId, rulesetId, StringComparison.Ordinal));
        }

        public HubReviewRecord Upsert(OwnerScope owner, HubReviewRecord record)
        {
            int existingIndex = _records.FindIndex(current =>
                string.Equals(current.OwnerId, owner.NormalizedValue, StringComparison.Ordinal)
                && string.Equals(current.ProjectKind, record.ProjectKind, StringComparison.Ordinal)
                && string.Equals(current.ProjectId, record.ProjectId, StringComparison.Ordinal)
                && string.Equals(current.RulesetId, record.RulesetId, StringComparison.Ordinal));
            HubReviewRecord normalizedRecord = record with { OwnerId = owner.NormalizedValue };
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
