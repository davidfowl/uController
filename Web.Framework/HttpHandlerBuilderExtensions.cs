using Microsoft.AspNetCore.Builder;

namespace Web.Framework
{
    public static class MiddlewareControllerBuilderExtensions
    {
        public static IApplicationBuilder UseHttpHandler<THttpHandler>(this IApplicationBuilder app) where THttpHandler : HttpHandler
        {
            return app.Use(HttpHandler.Build<THttpHandler>());
        }
    }
}
