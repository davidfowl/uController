using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Web.Framework;

namespace Samples
{
    /// <summary>
    /// An example of what the generated C# binding code would be for the MyHandler class.
    /// </summary>
    public class MyHandler_Generated
    {
        // This only gets used/generated for [FromBody] methods
        private readonly IHttpRequestReader _reader;

        // This only gets used/generated if there's the type is activated via DI
        private readonly ObjectFactory _factory;

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
            _factory = ActivatorUtilities.CreateFactory(typeof(MyHandler), Type.EmptyTypes);

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
            var generated = ActivatorUtilities.CreateInstance<MyHandler_Generated>(builder.ServiceProvider);

            builder.Map("/", generated.Get_Delegate, new HttpGetAttribute());
            builder.Map("/blah", generated.Blah_Delegate, new HttpGetAttribute());
            builder.Map("/status/{status}", generated.StatusCode_Delegate, new HttpGetAttribute());
            builder.Map("/slow/status/{status}", generated.SlowTaskStatusCode_Delegate, new HttpGetAttribute());
            builder.Map("/fast/status/{status}", generated.FastValueTaskStatusCode_Delegate, new HttpGetAttribute());
            builder.Map("/lag", generated.DoAsync_Delegate, new HttpGetAttribute());
            builder.Map("/hey/david", generated.HelloDavid_Delegate, new HttpGetAttribute());
            builder.Map("/hey/{name?}", generated.GetAsync_Delegate, new HttpGetAttribute());
            builder.Map("/hello", generated.Hello_Delegate, new HttpGetAttribute());
            builder.Map("/", generated.Post_Delegate, new HttpPostAttribute());
            builder.Map("/post-form", generated.PostAForm_Delegate, new HttpPostAttribute());
            builder.Map("/auth", generated.Authed_Delegate, new HttpPostAttribute(), new AuthorizeAttribute());
        }
    }
}
