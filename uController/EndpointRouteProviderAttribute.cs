using System;

namespace Microsoft.AspNetCore.Routing
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public sealed class EndpointRouteProviderAttribute : Attribute
    {
        public Type RouteProviderType { get; }

        public Type HandlerType { get; set; }

        public EndpointRouteProviderAttribute(Type routeProviderType, Type handlerType)
        {
            RouteProviderType = routeProviderType;
            HandlerType = handlerType;
        }
    }
}
