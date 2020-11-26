using Microsoft.AspNetCore.Routing;
using uController;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointRouteBuilderExtensions
    {
        public static void MapHttpHandler<THttpHandler>(this IEndpointRouteBuilder builder)
        {
            HttpHandlerBuilder.Build<THttpHandler>(builder);
        }
    }
}
