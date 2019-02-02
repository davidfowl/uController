using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing
{
    public static class EndpointRouteBuilderExtensions
    {
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
