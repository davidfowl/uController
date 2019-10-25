using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing
{
    public static class EndpointRouteBuilderExtensions
    {
        private static Lazy<Dictionary<Type, Action<IEndpointRouteBuilder>>> _routeProviders = new Lazy<Dictionary<Type, Action<IEndpointRouteBuilder>>>(CreateRouteProviderMapping);

        public static void MapHttpHandler<T>(this IEndpointRouteBuilder routes)
        {
            if (!_routeProviders.Value.TryGetValue(typeof(T), out var routeProvider))
            {
                throw new InvalidOperationException($"Route mapping for {typeof(T)} could not be found");
            }

            routeProvider(routes);
        }

        private static Dictionary<Type, Action<IEndpointRouteBuilder>> CreateRouteProviderMapping()
        {
            var mapping = new Dictionary<Type, Action<IEndpointRouteBuilder>>();

            foreach (EndpointRouteProviderAttribute attribute in Assembly.GetEntryAssembly().GetCustomAttributes(typeof(EndpointRouteProviderAttribute), inherit: false))
            {
                bool initialized = false;
                mapping[attribute.HandlerType] = (routes) =>
                {
                    if (!initialized)
                    {
                        var provider = (IEndpointRouteProvider)ActivatorUtilities.CreateInstance(routes.ServiceProvider, attribute.RouteProviderType);
                        provider.MapRoutes(routes);
                        initialized = true;
                    }
                };
            }

            return mapping;
        }
    }
}
