
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{
    delegate Microsoft.AspNetCore.Http.RequestDelegate RequestDelegateFactoryFunc(System.Delegate handler, Microsoft.AspNetCore.Builder.EndpointBuilder builder);

    public static class MapActionsExtensions
    {
        private static readonly System.Collections.Generic.Dictionary<(string, int), RequestDelegateFactoryFunc> map = new();
        static MapActionsExtensions()
        {
            map[(@"C:\dev\git\uController\samples\Program.cs", 11)] = (del, builder) =>
            {
                var handler = (System.Func<string>)del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {
                    filteredInvocation = BuildFilterDelegate(ic =>
                    {
                        return System.Threading.Tasks.ValueTask.FromResult<object>(handler());
                    },
                    builder,
                    handler.Method);
                }

                System.Threading.Tasks.Task RequestHandler(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var result = handler();
                    return httpContext.Response.WriteAsync(result);
                }

                async System.Threading.Tasks.Task RequestHandlerFiltered(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var result = await filteredInvocation(new DefaultEndpointFilterInvocationContext(httpContext));
                    if (result is IResult r)
                    {
                        await r.ExecuteAsync(httpContext);
                    }
                    else if (result is string s)
                    {
                        await httpContext.Response.WriteAsync(s);
                    }
                    else
                    {
                        await httpContext.Response.WriteAsJsonAsync(result);
                    }
                }

                return filteredInvocation is null ? RequestHandler : RequestHandlerFiltered;
            };

            map[(@"C:\dev\git\uController\samples\Program.cs", 12)] = (del, builder) =>
            {
                var handler = (System.Func<string, string>)del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {
                    filteredInvocation = BuildFilterDelegate(ic =>
                    {
                        return System.Threading.Tasks.ValueTask.FromResult<object>(handler(ic.GetArgument<string>(0)));
                    },
                    builder,
                    handler.Method);
                }

                System.Threading.Tasks.Task RequestHandler(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var arg_name = httpContext.Request.RouteValues["name"]?.ToString();
                    var result = handler(arg_name);
                    return httpContext.Response.WriteAsync(result);
                }

                async System.Threading.Tasks.Task RequestHandlerFiltered(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var arg_name = httpContext.Request.RouteValues["name"]?.ToString();
                    var result = await filteredInvocation(new DefaultEndpointFilterInvocationContext(httpContext, arg_name));
                    if (result is IResult r)
                    {
                        await r.ExecuteAsync(httpContext);
                    }
                    else if (result is string s)
                    {
                        await httpContext.Response.WriteAsync(s);
                    }
                    else
                    {
                        await httpContext.Response.WriteAsJsonAsync(result);
                    }
                }

                return filteredInvocation is null ? RequestHandler : RequestHandlerFiltered;
            };

            map[(@"C:\dev\git\uController\samples\Program.cs", 14)] = (del, builder) =>
            {
                var handler = (System.Func<Person>)del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {
                    filteredInvocation = BuildFilterDelegate(ic =>
                    {
                        return System.Threading.Tasks.ValueTask.FromResult<object>(handler());
                    },
                    builder,
                    handler.Method);
                }

                System.Threading.Tasks.Task RequestHandler(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var result = handler();
                    return httpContext.Response.WriteAsJsonAsync(result);
                }

                async System.Threading.Tasks.Task RequestHandlerFiltered(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var result = await filteredInvocation(new DefaultEndpointFilterInvocationContext(httpContext));
                    if (result is IResult r)
                    {
                        await r.ExecuteAsync(httpContext);
                    }
                    else if (result is string s)
                    {
                        await httpContext.Response.WriteAsync(s);
                    }
                    else
                    {
                        await httpContext.Response.WriteAsJsonAsync(result);
                    }
                }

                return filteredInvocation is null ? RequestHandler : RequestHandlerFiltered;
            };

            map[(@"C:\dev\git\uController\samples\Program.cs", 16)] = (del, builder) =>
            {
                var handler = (System.Func<System.Security.Claims.ClaimsPrincipal, ISayHello, Microsoft.AspNetCore.Http.IResult>)del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {
                    filteredInvocation = BuildFilterDelegate(ic =>
                    {
                        return System.Threading.Tasks.ValueTask.FromResult<object>(handler(ic.GetArgument<System.Security.Claims.ClaimsPrincipal>(0), ic.GetArgument<ISayHello>(1)));
                    },
                    builder,
                    handler.Method);
                }

                System.Threading.Tasks.Task RequestHandler(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    System.Security.Claims.ClaimsPrincipal arg_c = default;
                    var arg_s = httpContext.RequestServices.GetRequiredService<ISayHello>();
                    var result = handler(arg_c, arg_s);
                    return result.ExecuteAsync(httpContext);
                }

                async System.Threading.Tasks.Task RequestHandlerFiltered(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    System.Security.Claims.ClaimsPrincipal arg_c = default;
                    var arg_s = httpContext.RequestServices.GetRequiredService<ISayHello>();
                    var result = await filteredInvocation(new DefaultEndpointFilterInvocationContext(httpContext, arg_c, arg_s));
                    if (result is IResult r)
                    {
                        await r.ExecuteAsync(httpContext);
                    }
                    else if (result is string s)
                    {
                        await httpContext.Response.WriteAsync(s);
                    }
                    else
                    {
                        await httpContext.Response.WriteAsJsonAsync(result);
                    }
                }

                return filteredInvocation is null ? RequestHandler : RequestHandlerFiltered;
            };

            map[(@"C:\dev\git\uController\samples\Program.cs", 18)] = (del, builder) =>
            {
                var handler = (System.Func<System.Text.Json.Nodes.JsonNode, System.Text.Json.Nodes.JsonNode>)del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {
                    filteredInvocation = BuildFilterDelegate(ic =>
                    {
                        return System.Threading.Tasks.ValueTask.FromResult<object>(handler(ic.GetArgument<System.Text.Json.Nodes.JsonNode>(0)));
                    },
                    builder,
                    handler.Method);
                }

                async System.Threading.Tasks.Task RequestHandler(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var arg_node = await httpContext.Request.ReadFromJsonAsync<System.Text.Json.Nodes.JsonNode>();
                    var result = handler(arg_node);
                    await httpContext.Response.WriteAsJsonAsync(result);
                }

                async System.Threading.Tasks.Task RequestHandlerFiltered(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var arg_node = await httpContext.Request.ReadFromJsonAsync<System.Text.Json.Nodes.JsonNode>();
                    var result = await filteredInvocation(new DefaultEndpointFilterInvocationContext(httpContext, arg_node));
                    if (result is IResult r)
                    {
                        await r.ExecuteAsync(httpContext);
                    }
                    else if (result is string s)
                    {
                        await httpContext.Response.WriteAsync(s);
                    }
                    else
                    {
                        await httpContext.Response.WriteAsJsonAsync(result);
                    }
                }

                return filteredInvocation is null ? RequestHandler : RequestHandlerFiltered;
            };

            map[(@"C:\dev\git\uController\samples\Program.cs", 23)] = (del, builder) =>
            {
                var handler = (System.Func<Model, Model>)del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {
                    filteredInvocation = BuildFilterDelegate(ic =>
                    {
                        return System.Threading.Tasks.ValueTask.FromResult<object>(handler(ic.GetArgument<Model>(0)));
                    },
                    builder,
                    handler.Method);
                }

                async System.Threading.Tasks.Task RequestHandler(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var arg_m = await Model.BindAsync(httpContext);
                    var result = handler(arg_m);
                    await httpContext.Response.WriteAsJsonAsync(result);
                }

                async System.Threading.Tasks.Task RequestHandlerFiltered(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var arg_m = await Model.BindAsync(httpContext);
                    var result = await filteredInvocation(new DefaultEndpointFilterInvocationContext(httpContext, arg_m));
                    if (result is IResult r)
                    {
                        await r.ExecuteAsync(httpContext);
                    }
                    else if (result is string s)
                    {
                        await httpContext.Response.WriteAsync(s);
                    }
                    else
                    {
                        await httpContext.Response.WriteAsJsonAsync(result);
                    }
                }

                return filteredInvocation is null ? RequestHandler : RequestHandlerFiltered;
            };

            map[(@"C:\dev\git\uController\samples\Program.cs", 25)] = (del, builder) =>
            {
                var handler = (System.Func<System.Threading.CancellationToken, object>)del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {
                    filteredInvocation = BuildFilterDelegate(ic =>
                    {
                        return System.Threading.Tasks.ValueTask.FromResult<object>(handler(ic.GetArgument<System.Threading.CancellationToken>(0)));
                    },
                    builder,
                    handler.Method);
                }

                System.Threading.Tasks.Task RequestHandler(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var arg_ct = httpContext.RequestAborted;
                    var result = handler(arg_ct);
                    if (result is IResult r)
                    {
                        return r.ExecuteAsync(httpContext);
                    }
                    else if (result is string s)
                    {
                        return httpContext.Response.WriteAsync(s);
                    }
                    else
                    {
                        return httpContext.Response.WriteAsJsonAsync(result);
                    }
                }

                async System.Threading.Tasks.Task RequestHandlerFiltered(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var arg_ct = httpContext.RequestAborted;
                    var result = await filteredInvocation(new DefaultEndpointFilterInvocationContext(httpContext, arg_ct));
                    if (result is IResult r)
                    {
                        await r.ExecuteAsync(httpContext);
                    }
                    else if (result is string s)
                    {
                        await httpContext.Response.WriteAsync(s);
                    }
                    else
                    {
                        await httpContext.Response.WriteAsJsonAsync(result);
                    }
                }

                return filteredInvocation is null ? RequestHandler : RequestHandlerFiltered;
            };

            map[(@"C:\dev\git\uController\samples\Program.cs", 29)] = (del, builder) =>
            {
                var handler = (System.Func<int?, Microsoft.AspNetCore.Http.IResult>)del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {
                    filteredInvocation = BuildFilterDelegate(ic =>
                    {
                        return System.Threading.Tasks.ValueTask.FromResult<object>(handler(ic.GetArgument<int?>(0)));
                    },
                    builder,
                    handler.Method);
                }

                System.Threading.Tasks.Task RequestHandler(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    System.Nullable<int> arg_id = default;
                    var result = handler(arg_id);
                    return result.ExecuteAsync(httpContext);
                }

                async System.Threading.Tasks.Task RequestHandlerFiltered(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    System.Nullable<int> arg_id = default;
                    var result = await filteredInvocation(new DefaultEndpointFilterInvocationContext(httpContext, arg_id));
                    if (result is IResult r)
                    {
                        await r.ExecuteAsync(httpContext);
                    }
                    else if (result is string s)
                    {
                        await httpContext.Response.WriteAsync(s);
                    }
                    else
                    {
                        await httpContext.Response.WriteAsJsonAsync(result);
                    }
                }

                return filteredInvocation is null ? RequestHandler : RequestHandlerFiltered;
            };

            map[(@"C:\dev\git\uController\samples\Program.cs", 31)] = (del, builder) =>
            {
                var handler = (System.Func<Microsoft.AspNetCore.Http.HttpRequest, Microsoft.AspNetCore.Http.HttpResponse, System.Threading.Tasks.Task>)del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {
                    filteredInvocation = BuildFilterDelegate(ic =>
                    {
                        return System.Threading.Tasks.ValueTask.FromResult<object>(handler(ic.GetArgument<Microsoft.AspNetCore.Http.HttpRequest>(0), ic.GetArgument<Microsoft.AspNetCore.Http.HttpResponse>(1)));
                    },
                    builder,
                    handler.Method);
                }

                System.Threading.Tasks.Task RequestHandler(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var arg_req = httpContext.Request;
                    var arg_resp = httpContext.Response;
                    return handler(arg_req, arg_resp);
                }

                async System.Threading.Tasks.Task RequestHandlerFiltered(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var arg_req = httpContext.Request;
                    var arg_resp = httpContext.Response;
                    var result = await filteredInvocation(new DefaultEndpointFilterInvocationContext(httpContext, arg_req, arg_resp));
                    if (result is IResult r)
                    {
                        await r.ExecuteAsync(httpContext);
                    }
                    else if (result is string s)
                    {
                        await httpContext.Response.WriteAsync(s);
                    }
                    else
                    {
                        await httpContext.Response.WriteAsJsonAsync(result);
                    }
                }

                return filteredInvocation is null ? RequestHandler : RequestHandlerFiltered;
            };

            map[(@"C:\dev\git\uController\samples\Program.cs", 36)] = (del, builder) =>
            {
                var handler = (System.Func<string>)del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {
                    filteredInvocation = BuildFilterDelegate(ic =>
                    {
                        return System.Threading.Tasks.ValueTask.FromResult<object>(handler());
                    },
                    builder,
                    handler.Method);
                }

                System.Threading.Tasks.Task RequestHandler(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var result = handler();
                    return httpContext.Response.WriteAsync(result);
                }

                async System.Threading.Tasks.Task RequestHandlerFiltered(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var result = await filteredInvocation(new DefaultEndpointFilterInvocationContext(httpContext));
                    if (result is IResult r)
                    {
                        await r.ExecuteAsync(httpContext);
                    }
                    else if (result is string s)
                    {
                        await httpContext.Response.WriteAsync(s);
                    }
                    else
                    {
                        await httpContext.Response.WriteAsJsonAsync(result);
                    }
                }

                return filteredInvocation is null ? RequestHandler : RequestHandlerFiltered;
            };

            map[(@"C:\dev\git\uController\samples\Program.cs", 51)] = (del, builder) =>
            {
                var handler = (System.Func<string>)del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {
                    filteredInvocation = BuildFilterDelegate(ic =>
                    {
                        return System.Threading.Tasks.ValueTask.FromResult<object>(handler());
                    },
                    builder,
                    handler.Method);
                }

                System.Threading.Tasks.Task RequestHandler(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var result = handler();
                    return httpContext.Response.WriteAsync(result);
                }

                async System.Threading.Tasks.Task RequestHandlerFiltered(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    var result = await filteredInvocation(new DefaultEndpointFilterInvocationContext(httpContext));
                    if (result is IResult r)
                    {
                        await r.ExecuteAsync(httpContext);
                    }
                    else if (result is string s)
                    {
                        await httpContext.Response.WriteAsync(s);
                    }
                    else
                    {
                        await httpContext.Response.WriteAsJsonAsync(result);
                    }
                }

                return filteredInvocation is null ? RequestHandler : RequestHandlerFiltered;
            };

        }

        internal static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapGet(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, string pattern, System.Func<string> handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return MapCore(routes, pattern, handler, static (r, p, h) => r.MapGet(p, h), filePath, lineNumber);
        }

        internal static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapGet(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, string pattern, System.Func<string, string> handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return MapCore(routes, pattern, handler, static (r, p, h) => r.MapGet(p, h), filePath, lineNumber);
        }

        internal static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapGet(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, string pattern, System.Func<Person> handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return MapCore(routes, pattern, handler, static (r, p, h) => r.MapGet(p, h), filePath, lineNumber);
        }

        internal static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapGet(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, string pattern, System.Func<System.Security.Claims.ClaimsPrincipal, ISayHello, Microsoft.AspNetCore.Http.IResult> handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return MapCore(routes, pattern, handler, static (r, p, h) => r.MapGet(p, h), filePath, lineNumber);
        }

        internal static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapPost(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, string pattern, System.Func<System.Text.Json.Nodes.JsonNode, System.Text.Json.Nodes.JsonNode> handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return MapCore(routes, pattern, handler, static (r, p, h) => r.MapPost(p, h), filePath, lineNumber);
        }

        internal static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapPost(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, string pattern, System.Func<Model, Model> handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return MapCore(routes, pattern, handler, static (r, p, h) => r.MapPost(p, h), filePath, lineNumber);
        }

        internal static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapGet(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, string pattern, System.Func<System.Threading.CancellationToken, object> handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return MapCore(routes, pattern, handler, static (r, p, h) => r.MapGet(p, h), filePath, lineNumber);
        }

        internal static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder Map(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, string pattern, System.Func<int?, Microsoft.AspNetCore.Http.IResult> handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return MapCore(routes, pattern, handler, static (r, p, h) => r.Map(p, h), filePath, lineNumber);
        }

        internal static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapPatch(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, string pattern, System.Func<Microsoft.AspNetCore.Http.HttpRequest, Microsoft.AspNetCore.Http.HttpResponse, System.Threading.Tasks.Task> handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return MapCore(routes, pattern, handler, static (r, p, h) => r.MapPatch(p, h), filePath, lineNumber);
        }

        private static Microsoft.AspNetCore.Builder.RouteHandlerBuilder MapCore(
            this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes,
            string pattern,
            System.Delegate handler,
            Func<Microsoft.AspNetCore.Routing.IEndpointRouteBuilder, string, System.Delegate, Microsoft.AspNetCore.Builder.RouteHandlerBuilder> mapper,
            string filePath,
            int lineNumber)
        {
            var factory = map[(filePath, lineNumber)];
            var conventionBuilder = mapper(routes, pattern, handler);
            conventionBuilder.Finally(e =>
            {
                e.RequestDelegate = factory(handler, e);
            });

            return conventionBuilder;
        }

        private static EndpointFilterDelegate BuildFilterDelegate(EndpointFilterDelegate filteredInvocation, EndpointBuilder builder, System.Reflection.MethodInfo mi)
        {
            var routeHandlerFilters = builder.FilterFactories;

            var context0 = new EndpointFilterFactoryContext
            {
                MethodInfo = mi,
                ApplicationServices = builder.ApplicationServices,
            };

            var initialFilteredInvocation = filteredInvocation;

            for (var i = routeHandlerFilters.Count - 1; i >= 0; i--)
            {
                var filterFactory = routeHandlerFilters[i];
                filteredInvocation = filterFactory(context0, filteredInvocation);
            }

            return filteredInvocation;
        }
    }
}