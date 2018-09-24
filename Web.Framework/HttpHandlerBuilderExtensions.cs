using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Web.Framework
{
    public static class MiddlewareControllerBuilderExtensions
    {
        public static void MapHttpHandler<THttpHandler>(this IEndpointRouteBuilder builder, Action<HttpModel> configure = null)
        {
            var sdfsf = HttpHandler.Build<THttpHandler>(configure);
            //return builder.Use();
        }
    }
}
