using Chummer.Contracts.Hub;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Rulesets;

namespace Chummer.Application.Hub;

public sealed class DefaultHubReviewService : IHubReviewService
{
    private readonly IHubReviewStore _reviewStore;

    public DefaultHubReviewService(IHubReviewStore reviewStore)
    {
        _reviewStore = reviewStore;
    }

    public HubPublicationResult<HubReviewCatalog> ListReviews(OwnerScope owner, string? kind = null, string? itemId = null, string? rulesetId = null)
    {
        HubReviewReceipt[] items = _reviewStore.List(owner, NormalizeOptional(kind), NormalizeOptional(itemId), RulesetDefaults.NormalizeOptional(rulesetId))
            .OrderByDescending(record => record.UpdatedAtUtc)
            .Select(ToReceipt)
            .ToArray();
        return HubPublicationResult<HubReviewCatalog>.Implemented(new HubReviewCatalog(items));
    }

    public HubPublicationResult<HubReviewReceipt> UpsertReview(OwnerScope owner, string kind, string itemId, HubUpsertReviewRequest? request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string normalizedKind = NormalizeRequired(kind);
        string normalizedItemId = NormalizeItemId(itemId);
        string normalizedRulesetId = RulesetDefaults.NormalizeRequired(request.RulesetId);
        string normalizedRecommendation = NormalizeRecommendationState(request.RecommendationState);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        HubReviewRecord? existing = _reviewStore.Get(owner, normalizedKind, normalizedItemId, normalizedRulesetId);
        HubReviewRecord persisted = _reviewStore.Upsert(
            owner,
            new HubReviewRecord(
                ReviewId: existing?.ReviewId ?? $"review-{Guid.NewGuid():N}",
                ProjectKind: normalizedKind,
                ProjectId: normalizedItemId,
                RulesetId: normalizedRulesetId,
                OwnerId: owner.NormalizedValue,
                RecommendationState: normalizedRecommendation,
                CreatedAtUtc: existing?.CreatedAtUtc ?? now,
                UpdatedAtUtc: now,
                Stars: request.Stars,
                ReviewText: NormalizeOptional(request.ReviewText),
                UsedAtTable: request.UsedAtTable));
        return HubPublicationResult<HubReviewReceipt>.Implemented(ToReceipt(persisted));
    }

    private static HubReviewReceipt ToReceipt(HubReviewRecord record)
        => new(
            ReviewId: record.ReviewId,
            ProjectKind: record.ProjectKind,
            ProjectId: record.ProjectId,
            RulesetId: record.RulesetId,
            OwnerId: record.OwnerId,
            RecommendationState: record.RecommendationState,
            CreatedAtUtc: record.CreatedAtUtc,
            UpdatedAtUtc: record.UpdatedAtUtc,
            Stars: record.Stars,
            ReviewText: record.ReviewText,
            UsedAtTable: record.UsedAtTable);

    private static string NormalizeRequired(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim().ToLowerInvariant();
    }

    private static string NormalizeItemId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    private static string NormalizeRecommendationState(string value)
    {
        string normalized = NormalizeRequired(value);
        return normalized switch
        {
            HubRecommendationStates.Recommended => HubRecommendationStates.Recommended,
            HubRecommendationStates.Neutral => HubRecommendationStates.Neutral,
            HubRecommendationStates.NotRecommended => HubRecommendationStates.NotRecommended,
            _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unsupported recommendation state '{value}'.")
        };
    }
}
