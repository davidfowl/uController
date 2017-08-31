using System;
using Microsoft.AspNetCore.Builder;

namespace Web.Framework
{
    public static class MiddlewareControllerBuilderExtensions
    {
        public static IApplicationBuilder UseHttpHandler<THttpHandler>(this IApplicationBuilder app, Action<HttpModel> configure = null)
        {
            return app.Use(HttpHandler.Build<THttpHandler>(configure));
        }
    }
}
