using Chummer.Contracts.Hub;
using Chummer.Contracts.Owners;

namespace Chummer.Application.Hub;

public sealed class NotImplementedHubModerationService : IHubModerationService
{
    public HubPublicationResult<HubModerationQueue> ListQueue(OwnerScope owner, string? state = null)
        => HubPublicationResult<HubModerationQueue>.FromNotImplemented(
            new HubPublicationNotImplementedReceipt(
                Error: "hub_publication_not_implemented",
                Operation: HubPublicationOperations.ListModerationQueue,
                Message: "The dedicated ChummerHub moderation workflow is not implemented yet.",
                OwnerId: owner.NormalizedValue));
}
