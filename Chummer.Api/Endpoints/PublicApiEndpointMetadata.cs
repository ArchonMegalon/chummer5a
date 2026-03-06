namespace Chummer.Api.Endpoints;

internal sealed class PublicApiEndpointMetadata;

internal static class PublicApiEndpointConventionBuilderExtensions
{
    public static TBuilder AllowPublicApiKeyBypass<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.WithMetadata(new PublicApiEndpointMetadata());
        return builder;
    }
}
