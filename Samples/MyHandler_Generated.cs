using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using Web.Framework;

namespace Samples
{
    /// <summary>
    /// An example of what the generated C# binding code would be for the MyHandler class. This
    /// </summary>
    public class MyHandler_Generated
    {
        private readonly IHttpRequestReader _reader;

        public readonly RequestDelegate Get_Delegate;
        public readonly RequestDelegate Blah_Delegate;
        public readonly RequestDelegate StatusCode_Delegate;
        public readonly RequestDelegate SlowTaskStatusCode_Delegate;
        public readonly RequestDelegate FastValueTaskStatusCode_Delegate;
        public readonly RequestDelegate DoAsync_Delegate;
        public readonly RequestDelegate HelloDavid_Delegate;
        public readonly RequestDelegate GetAsync_Delegate;
        public readonly RequestDelegate Hello_Delegate;
        public readonly RequestDelegate Post_Delegate;
        public readonly RequestDelegate Authed_Delegate;
        public readonly RequestDelegate PostAForm_Delegate;

        public MyHandler_Generated(IHttpRequestReader reader)
        {
            _reader = reader;

            Get_Delegate = Get;
            Blah_Delegate = Blah;
            StatusCode_Delegate = StatusCode;
            SlowTaskStatusCode_Delegate = SlowTaskStatusCode;
            FastValueTaskStatusCode_Delegate = FastValueTaskStatusCode;
            DoAsync_Delegate = DoAsync;
            HelloDavid_Delegate = HelloDavid;
            GetAsync_Delegate = GetAsync;
            Hello_Delegate = Hello;
            Post_Delegate = Post;
            Authed_Delegate = Authed;
            PostAForm_Delegate = PostAForm;
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
            await httpContext.Request.ReadFormAsync();
            handler.PostAForm(httpContext.Request.Form);
        }
    }

    public static class MyHandlerRoutingExtensions
    {
        public static void MapMyHandler(this IEndpointRouteBuilder builder)
        {
            var dataSource = builder.DataSources.OfType<HandlerEndpointsDataSource>().SingleOrDefault();
            if (dataSource == null)
            {
                dataSource = new HandlerEndpointsDataSource();
                builder.DataSources.Add(dataSource);
            }

            var generated = ActivatorUtilities.CreateInstance<MyHandler_Generated>(builder.ServiceProvider);

            dataSource.AddEndpoints(new List<Endpoint> {
                new RouteEndpointBuilder(generated.GetAsync_Delegate, RoutePatternFactory.Parse("/"), 0).Build(),
                new RouteEndpointBuilder(generated.Blah_Delegate, RoutePatternFactory.Parse("/blah"), 0).Build(),
                new RouteEndpointBuilder(generated.StatusCode_Delegate, RoutePatternFactory.Parse("/status/{status}"), 0).Build(),
                new RouteEndpointBuilder(generated.SlowTaskStatusCode_Delegate, RoutePatternFactory.Parse("/slow/status/{status}"), 0).Build(),
                new RouteEndpointBuilder(generated.FastValueTaskStatusCode_Delegate, RoutePatternFactory.Parse("/fast/status/{status}"), 0).Build(),
                new RouteEndpointBuilder(generated.DoAsync_Delegate, RoutePatternFactory.Parse("/lag"), 0).Build(),
                new RouteEndpointBuilder(generated.HelloDavid_Delegate, RoutePatternFactory.Parse("/hey/david"), 0).Build(),
                new RouteEndpointBuilder(generated.GetAsync_Delegate, RoutePatternFactory.Parse("/hey/{name?}"), 0).Build(),
                new RouteEndpointBuilder(generated.Hello_Delegate, RoutePatternFactory.Parse("/hello"), 0).Build(),
                new RouteEndpointBuilder(generated.Post_Delegate, RoutePatternFactory.Parse("/"), 0).Build(),
                new RouteEndpointBuilder(generated.PostAForm_Delegate, RoutePatternFactory.Parse("/post-form"), 0).Build(),
                new RouteEndpointBuilder(generated.Authed_Delegate, RoutePatternFactory.Parse("/auth"), 0).Build(),
            });
        }

        private class HandlerEndpointsDataSource : EndpointDataSource
        {
            private readonly List<Endpoint> _endpoints = new List<Endpoint>();

            public void AddEndpoints(IList<Endpoint> endpoints)
            {
                _endpoints.AddRange(endpoints);
            }

            public override IReadOnlyList<Endpoint> Endpoints => _endpoints;

            public override IChangeToken GetChangeToken()
            {
                return NullChangeToken.Singleton;
            }
        }
    }
}
