using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Samples;
using Web.Framework;

// This assembly attribute is part of the generated code to help register the routes
[assembly: EndpointRouteProvider(typeof(MyHandlerRouteProvider))]

namespace Samples
{
    /// <summary>
    /// An example of what the generated C# binding code would be for the MyHandler class.
    /// </summary>
    public class MyHandlerRouteProvider : IEndpointRouteProvider
    {
        // This only gets used/generated for [FromBody] methods
        private readonly IHttpRequestReader _reader;

        // This only gets used/generated if there's the type is activated via DI
        private readonly ObjectFactory _factory;

        public MyHandlerRouteProvider(IHttpRequestReader reader)
        {
            _reader = reader;
            _factory = ActivatorUtilities.CreateFactory(typeof(MyHandler), Type.EmptyTypes);
        }

        [DebuggerStepThrough]
        private Task Get(HttpContext httpContext)
        {
            var handler = new MyHandler();
            return handler.Get(httpContext);
        }

        [DebuggerStepThrough]
        private Task Blah(HttpContext httpContext)
        {
            var handler = new MyHandler();
            return new ObjectResult(handler.Blah()).ExecuteAsync(httpContext);
        }

        [DebuggerStepThrough]
        private Task StatusCode(HttpContext httpContext)
        {
            var handler = new MyHandler();
            var statusValue = (string)httpContext.Request.RouteValues["status"];
            int? status = null;

            if (statusValue != null && Int32.TryParse(statusValue, out var val))
            {
                status = val;
            }

            return handler.StatusCode(status ?? 0).ExecuteAsync(httpContext);
        }

        [DebuggerStepThrough]
        private async Task SlowTaskStatusCode(HttpContext httpContext)
        {
            var handler = new MyHandler();
            var result = await handler.SlowTaskStatusCode();
            await result.ExecuteAsync(httpContext);
        }

        [DebuggerStepThrough]
        private async Task FastValueTaskStatusCode(HttpContext httpContext)
        {
            var handler = new MyHandler();
            var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var result = await handler.FastValueTaskStatusCode(loggerFactory);
            await result.ExecuteAsync(httpContext);
        }

        [DebuggerStepThrough]
        private Task DoAsync(HttpContext httpContext)
        {
            var handler = new MyHandler();
            return handler.DoAsync(httpContext, httpContext.Request.Query["q"]);
        }

        [DebuggerStepThrough]
        private Task HelloDavid(HttpContext httpContext)
        {
            var handler = new MyHandler();
            var result = handler.HelloDavid();
            return new ObjectResult(result).ExecuteAsync(httpContext);
        }

        [DebuggerStepThrough]
        private async Task GetAsync(HttpContext httpContext)
        {
            var handler = new MyHandler();
            var name = (string)httpContext.Request.RouteValues["name"];
            var result = await handler.GetAsync(name);
            await new ObjectResult(result).ExecuteAsync(httpContext);
        }

        [DebuggerStepThrough]
        private Task Hello(HttpContext httpContext)
        {
            var handler = new MyHandler();
            var result = handler.Hello();
            return new ObjectResult(result).ExecuteAsync(httpContext);
        }

        [DebuggerStepThrough]
        private async Task Post(HttpContext httpContext)
        {
            var handler = new MyHandler();

            var bodyValue = (JsonDocument)await _reader.ReadAsync(httpContext, typeof(JsonDocument));

            var result = handler.Post(bodyValue);
            await result.ExecuteAsync(httpContext);
        }

        [DebuggerStepThrough]
        private Task Authed(HttpContext httpContext)
        {
            var handler = new MyHandler();
            handler.Authed();
            return Task.CompletedTask;
        }

        [DebuggerStepThrough]
        private async Task PostAForm(HttpContext httpContext)
        {
            var handler = new MyHandler();
            var form = await httpContext.Request.ReadFormAsync();
            handler.PostAForm(form);
        }

        public void MapRoutes(IEndpointRouteBuilder routes)
        {
            routes.Map("/", Get).WithMetadata(new HttpGetAttribute());
            routes.Map("/blah", Blah).WithMetadata(new HttpGetAttribute());
            routes.Map("/status/{status}", StatusCode).WithMetadata(new HttpGetAttribute());
            routes.Map("/slow/status/{status}", SlowTaskStatusCode).WithMetadata(new HttpGetAttribute());
            routes.Map("/fast/status/{status}", FastValueTaskStatusCode).WithMetadata(new HttpGetAttribute());
            routes.Map("/lag", DoAsync).WithMetadata(new HttpGetAttribute());
            routes.Map("/hey/david", HelloDavid).WithMetadata(new HttpGetAttribute());
            routes.Map("/hey/{name?}", GetAsync).WithMetadata(new HttpGetAttribute());
            routes.Map("/hello", Hello).WithMetadata(new HttpGetAttribute());
            routes.Map("/", Post).WithMetadata(new HttpPostAttribute());
            routes.Map("/post-form", PostAForm).WithMetadata(new HttpPostAttribute());
            routes.Map("/auth", Authed).WithMetadata(new HttpPostAttribute(), new AuthorizeAttribute());
        }
    }
}
