using Chummer.Contracts.Hub;
using Chummer.Contracts.Owners;

namespace Chummer.Application.Hub;

public sealed class DefaultHubModerationService : IHubModerationService
{
    private readonly IHubModerationCaseStore _moderationCaseStore;

    public DefaultHubModerationService(IHubModerationCaseStore moderationCaseStore)
    {
        _moderationCaseStore = moderationCaseStore;
    }

    public HubPublicationResult<HubModerationQueue> ListQueue(OwnerScope owner, string? state = null)
    {
        IReadOnlyList<HubModerationQueueItem> items = _moderationCaseStore
            .List(owner, state: NormalizeStateOptional(state))
            .OrderByDescending(record => record.UpdatedAtUtc)
            .Select(record => new HubModerationQueueItem(
                CaseId: record.CaseId,
                DraftId: record.DraftId,
                ProjectKind: record.ProjectKind,
                ProjectId: record.ProjectId,
                RulesetId: record.RulesetId,
                Title: record.Title,
                OwnerId: record.OwnerId,
                State: record.State,
                CreatedAtUtc: record.CreatedAtUtc,
                Summary: record.Summary))
            .ToArray();

        return HubPublicationResult<HubModerationQueue>.Implemented(new HubModerationQueue(items));
    }

    private static string? NormalizeStateOptional(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
}
