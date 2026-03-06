using Chummer.Application.Hub;
using Chummer.Application.Owners;
using Chummer.Contracts.Hub;

namespace Chummer.Api.Endpoints;

public static class HubPublisherEndpoints
{
    public static IEndpointRouteBuilder MapHubPublisherEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/hub/publishers", (IHubPublisherService hubPublisherService, IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(hubPublisherService.ListPublishers(ownerContextAccessor.Current)));

        app.MapGet("/api/hub/publishers/{publisherId}", (string publisherId, IHubPublisherService hubPublisherService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            HubPublisherProfile? profile = hubPublisherService.GetPublisher(ownerContextAccessor.Current, publisherId).Payload;
            return profile is null
                ? Results.NotFound(new
                {
                    error = "hub_publisher_not_found",
                    publisherId
                })
                : Results.Ok(profile);
        });

        app.MapPut("/api/hub/publishers/{publisherId}", (string publisherId, HubUpdatePublisherRequest? request, IHubPublisherService hubPublisherService, IOwnerContextAccessor ownerContextAccessor) =>
        {
            if (request is null)
            {
                return Results.BadRequest(new
                {
                    error = "hub_publisher_request_required",
                    publisherId
                });
            }

            return ToResult(hubPublisherService.UpsertPublisher(ownerContextAccessor.Current, publisherId, request));
        });

        return app;
    }

    private static IResult ToResult<T>(HubPublicationResult<T> result)
    {
        if (result.IsImplemented)
        {
            return Results.Ok(result.Payload);
        }

        HubPublicationNotImplementedReceipt receipt = result.NotImplemented
            ?? throw new InvalidOperationException("Hub publisher result was not implemented but did not include a receipt.");
        return Results.Json(receipt, statusCode: StatusCodes.Status501NotImplemented);
    }
}
