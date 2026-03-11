using Chummer.Blazor;
using Chummer.Contracts.AI;

namespace Chummer.Blazor.Desktop;

internal sealed class DesktopWorkbenchCoachApiClient : IWorkbenchCoachApiClient
{
    private static readonly AiNotImplementedReceipt Receipt = new(
        Error: "coach_sidecar_unavailable",
        Operation: "workbench_coach_desktop",
        Message: "Coach sidecar is not configured in the desktop runtime yet.",
        RouteType: AiRouteTypes.Coach);

    public Task<WorkbenchCoachApiCallResult<AiGatewayStatusProjection>> GetStatusAsync(CancellationToken ct = default)
        => Task.FromResult(WorkbenchCoachApiCallResult<AiGatewayStatusProjection>.FromNotImplemented(501, Receipt));

    public Task<WorkbenchCoachApiCallResult<AiProviderHealthProjection[]>> ListProviderHealthAsync(string? routeType = null, CancellationToken ct = default)
        => Task.FromResult(WorkbenchCoachApiCallResult<AiProviderHealthProjection[]>.FromNotImplemented(501, Receipt with { RouteType = routeType ?? AiRouteTypes.Coach }));

    public Task<WorkbenchCoachApiCallResult<AiConversationAuditCatalogPage>> ListConversationAuditsAsync(
        string routeType,
        string? runtimeFingerprint = null,
        int maxCount = 3,
        CancellationToken ct = default)
        => Task.FromResult(WorkbenchCoachApiCallResult<AiConversationAuditCatalogPage>.FromNotImplemented(501, Receipt with { RouteType = routeType ?? AiRouteTypes.Coach }));
}
