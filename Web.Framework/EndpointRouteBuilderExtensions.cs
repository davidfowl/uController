using Microsoft.AspNetCore.Routing;

namespace Web.Framework
{
    public static class EndpointRouteBuilderExtensions
    {
        public static void MapHttpHandler<THttpHandler>(this IEndpointRouteBuilder builder)
        {
            HttpHandler.Build<THttpHandler>(builder);
        }
    }
}
