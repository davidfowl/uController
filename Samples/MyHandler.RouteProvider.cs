using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
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

        private readonly RequestDelegate _getDelegate;
        private readonly RequestDelegate _blahDelegate;
        private readonly RequestDelegate _statusCodeDelegate;
        private readonly RequestDelegate _slowTaskStatusCodeDelegate;
        private readonly RequestDelegate _fastValueTaskStatusCodeDelegate;
        private readonly RequestDelegate _doAsyncDelegate;
        private readonly RequestDelegate _helloDavidDelegate;
        private readonly RequestDelegate _getAsyncDelegate;
        private readonly RequestDelegate _helloDelegate;
        private readonly RequestDelegate _postDelegate;
        private readonly RequestDelegate _authedDelegate;
        private readonly RequestDelegate _postAFormDelegate;

        public MyHandlerRouteProvider(IHttpRequestReader reader)
        {
            _reader = reader;
            _factory = ActivatorUtilities.CreateFactory(typeof(MyHandler), Type.EmptyTypes);

            _getDelegate = Get;
            _blahDelegate = Blah;
            _statusCodeDelegate = StatusCode;
            _slowTaskStatusCodeDelegate = SlowTaskStatusCode;
            _fastValueTaskStatusCodeDelegate = FastValueTaskStatusCode;
            _doAsyncDelegate = DoAsync;
            _helloDavidDelegate = HelloDavid;
            _getAsyncDelegate = GetAsync;
            _helloDelegate = Hello;
            _postDelegate = Post;
            _authedDelegate = Authed;
            _postAFormDelegate = PostAForm;
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
            var result = await handler.FastValueTaskStatusCode();
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

            var bodyValue = (JValue)await _reader.ReadAsync(httpContext, typeof(JValue));

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
            routes.Map("/", _getDelegate, new HttpGetAttribute());
            routes.Map("/blah", _blahDelegate, new HttpGetAttribute());
            routes.Map("/status/{status}", _statusCodeDelegate, new HttpGetAttribute());
            routes.Map("/slow/status/{status}", _slowTaskStatusCodeDelegate, new HttpGetAttribute());
            routes.Map("/fast/status/{status}", _fastValueTaskStatusCodeDelegate, new HttpGetAttribute());
            routes.Map("/lag", _doAsyncDelegate, new HttpGetAttribute());
            routes.Map("/hey/david", _helloDavidDelegate, new HttpGetAttribute());
            routes.Map("/hey/{name?}", _getAsyncDelegate, new HttpGetAttribute());
            routes.Map("/hello", _helloDelegate, new HttpGetAttribute());
            routes.Map("/", _postDelegate, new HttpPostAttribute());
            routes.Map("/post-form", _postAFormDelegate, new HttpPostAttribute());
            routes.Map("/auth", _authedDelegate, new HttpPostAttribute(), new AuthorizeAttribute());
        }
    }
}
