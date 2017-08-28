using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Web.Framework
{
    public static class HttpHandlerRouteBuilderExtensions
    {
        public static IRouteBuilder MapHandler<THttpHandler>(this IRouteBuilder builder, string template) where THttpHandler : HttpHandler
        {
            RequestDelegate handler = HttpHandler.Build<THttpHandler>()(context => Task.CompletedTask);

            var route = new Route(
                new RouteHandler(handler),
                template,
                defaults: null,
                constraints: null,
                dataTokens: null,
                inlineConstraintResolver: GetConstraintResolver(builder));

            builder.Routes.Add(route);
            return builder;
        }

        private static IInlineConstraintResolver GetConstraintResolver(IRouteBuilder builder)
        {
            return builder.ServiceProvider.GetRequiredService<IInlineConstraintResolver>();
        }
    }
}
