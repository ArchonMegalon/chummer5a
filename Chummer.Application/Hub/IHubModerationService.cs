using Chummer.Contracts.Hub;
using Chummer.Contracts.Owners;

namespace Chummer.Application.Hub;

public interface IHubModerationService
{
    HubPublicationResult<HubModerationQueue> ListQueue(OwnerScope owner, string? state = null);
}
