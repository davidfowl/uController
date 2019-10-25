using uController;

namespace Microsoft.AspNetCore.Routing
{
    public static class EndpointRouteBuilderExtensions
    {
        public static void MapDynamicHttpHandler<THttpHandler>(this IEndpointRouteBuilder builder)
        {
            HttpHandlerBuilder.Build<THttpHandler>(builder);
        }
    }
}
