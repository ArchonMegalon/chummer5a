using Chummer.Contracts.Hub;
using Chummer.Contracts.Owners;

namespace Chummer.Application.Hub;

public sealed class NotImplementedHubPublicationService : IHubPublicationService
{
    public HubPublicationResult<HubPublishDraftReceipt> CreateDraft(OwnerScope owner, HubPublishDraftRequest? request)
        => NotImplemented<HubPublishDraftReceipt>(
            owner,
            HubPublicationOperations.CreateDraft,
            request?.ProjectKind,
            request?.ProjectId);

    public HubPublicationResult<HubProjectSubmissionReceipt> SubmitForReview(OwnerScope owner, string kind, string itemId, string? rulesetId, HubSubmitProjectRequest? request)
        => NotImplemented<HubProjectSubmissionReceipt>(
            owner,
            HubPublicationOperations.SubmitProject,
            kind,
            itemId);

    private static HubPublicationResult<T> NotImplemented<T>(OwnerScope owner, string operation, string? kind = null, string? itemId = null)
        => HubPublicationResult<T>.FromNotImplemented(
            new HubPublicationNotImplementedReceipt(
                Error: "hub_publication_not_implemented",
                Operation: operation,
                Message: "The dedicated ChummerHub publication workflow is not implemented yet.",
                ProjectKind: string.IsNullOrWhiteSpace(kind) ? null : kind,
                ProjectId: string.IsNullOrWhiteSpace(itemId) ? null : itemId,
                OwnerId: owner.NormalizedValue));
}
