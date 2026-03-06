using Chummer.Contracts.Hub;
using Chummer.Contracts.Owners;

namespace Chummer.Application.Hub;

public interface IHubPublicationService
{
    HubPublicationResult<HubPublishDraftReceipt> CreateDraft(OwnerScope owner, HubPublishDraftRequest? request);

    HubPublicationResult<HubProjectSubmissionReceipt> SubmitForReview(OwnerScope owner, string kind, string itemId, string? rulesetId, HubSubmitProjectRequest? request);
}
