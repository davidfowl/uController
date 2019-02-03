using Microsoft.Extensions.DependencyInjection;
using Web.Framework;

namespace Microsoft.AspNetCore.Routing
{
    public static class EndpointRouteBuilderExtensions
    {
        public static void MapHttpHandler<THttpHandler>(this IEndpointRouteBuilder builder)
        {
            HttpHandler.Build<THttpHandler>(builder);
        }

        public static void MapRouteProviders<T>(this IEndpointRouteBuilder routes)
        {
            foreach (EndpointRouteProviderAttribute attribute in typeof(T).Assembly.GetCustomAttributes(typeof(EndpointRouteProviderAttribute), inherit: false))
            {
                var provider = (IEndpointRouteProvider)ActivatorUtilities.CreateInstance(routes.ServiceProvider, attribute.RouteProviderType);
                provider.MapRoutes(routes);
            }
        }
    }
}
