using uController;

namespace Microsoft.AspNetCore.Routing
{
    public static class EndpointRouteBuilderExtensions
    {
        public static void MapHttpHandler<THttpHandler>(this IEndpointRouteBuilder builder)
        {
            HttpHandlerBuilder.Build<THttpHandler>(builder);
        }
    }
}
