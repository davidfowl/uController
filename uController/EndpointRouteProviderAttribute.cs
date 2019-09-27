using System;

namespace Microsoft.AspNetCore.Routing
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public sealed class EndpointRouteProviderAttribute : Attribute
    {
        public Type RouteProviderType { get; }

        public EndpointRouteProviderAttribute(Type routeProviderType)
        {
            RouteProviderType = routeProviderType;
        }
    }
}
