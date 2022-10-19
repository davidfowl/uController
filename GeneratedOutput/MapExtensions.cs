
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
            map[(@"C:\dev\git\uController\samples\Program.cs", 9)] = (del, builder) =>
            {
                var handler = (System.Func<string>)del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {
                    var routeHandlerFilters = builder.FilterFactories;
                    filteredInvocation = ic =>
                    {
                        return System.Threading.Tasks.ValueTask.FromResult<object>(handler());
                    };

                    var context0 = new EndpointFilterFactoryContext
                    {
                        MethodInfo = handler.Method,
                        ApplicationServices = builder.ApplicationServices,
                    };

                    var initialFilteredInvocation = filteredInvocation;

                    for (var i = routeHandlerFilters.Count - 1; i >= 0; i--)
                    {
                        var filterFactory = routeHandlerFilters[i];
                        filteredInvocation = filterFactory(context0, filteredInvocation);
                    }
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

            map[(@"C:\dev\git\uController\samples\Program.cs", 10)] = (del, builder) =>
            {
                var handler = (System.Func<string, string>)del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {
                    var routeHandlerFilters = builder.FilterFactories;
                    filteredInvocation = ic =>
                    {
                        return System.Threading.Tasks.ValueTask.FromResult<object>(handler(ic.GetArgument<string>(0)));
                    };

                    var context0 = new EndpointFilterFactoryContext
                    {
                        MethodInfo = handler.Method,
                        ApplicationServices = builder.ApplicationServices,
                    };

                    var initialFilteredInvocation = filteredInvocation;

                    for (var i = routeHandlerFilters.Count - 1; i >= 0; i--)
                    {
                        var filterFactory = routeHandlerFilters[i];
                        filteredInvocation = filterFactory(context0, filteredInvocation);
                    }
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

            map[(@"C:\dev\git\uController\samples\Program.cs", 12)] = (del, builder) =>
            {
                var handler = (System.Func<Person>)del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {
                    var routeHandlerFilters = builder.FilterFactories;
                    filteredInvocation = ic =>
                    {
                        return System.Threading.Tasks.ValueTask.FromResult<object>(handler());
                    };

                    var context0 = new EndpointFilterFactoryContext
                    {
                        MethodInfo = handler.Method,
                        ApplicationServices = builder.ApplicationServices,
                    };

                    var initialFilteredInvocation = filteredInvocation;

                    for (var i = routeHandlerFilters.Count - 1; i >= 0; i--)
                    {
                        var filterFactory = routeHandlerFilters[i];
                        filteredInvocation = filterFactory(context0, filteredInvocation);
                    }
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

            map[(@"C:\dev\git\uController\samples\Program.cs", 14)] = (del, builder) =>
            {
                var handler = (System.Func<System.Security.Claims.ClaimsPrincipal, Microsoft.AspNetCore.Http.IResult>)del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {
                    var routeHandlerFilters = builder.FilterFactories;
                    filteredInvocation = ic =>
                    {
                        return System.Threading.Tasks.ValueTask.FromResult<object>(handler(ic.GetArgument<System.Security.Claims.ClaimsPrincipal>(0)));
                    };

                    var context0 = new EndpointFilterFactoryContext
                    {
                        MethodInfo = handler.Method,
                        ApplicationServices = builder.ApplicationServices,
                    };

                    var initialFilteredInvocation = filteredInvocation;

                    for (var i = routeHandlerFilters.Count - 1; i >= 0; i--)
                    {
                        var filterFactory = routeHandlerFilters[i];
                        filteredInvocation = filterFactory(context0, filteredInvocation);
                    }
                }

                System.Threading.Tasks.Task RequestHandler(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    System.Security.Claims.ClaimsPrincipal arg_c = default;
                    var result = handler(arg_c);
                    return result.ExecuteAsync(httpContext);
                }

                async System.Threading.Tasks.Task RequestHandlerFiltered(Microsoft.AspNetCore.Http.HttpContext httpContext)
                {
                    System.Security.Claims.ClaimsPrincipal arg_c = default;
                    var result = await filteredInvocation(new DefaultEndpointFilterInvocationContext(httpContext, arg_c));
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
                var handler = (System.Func<System.Text.Json.Nodes.JsonNode, System.Text.Json.Nodes.JsonNode>)del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {
                    var routeHandlerFilters = builder.FilterFactories;
                    filteredInvocation = ic =>
                    {
                        return System.Threading.Tasks.ValueTask.FromResult<object>(handler(ic.GetArgument<System.Text.Json.Nodes.JsonNode>(0)));
                    };

                    var context0 = new EndpointFilterFactoryContext
                    {
                        MethodInfo = handler.Method,
                        ApplicationServices = builder.ApplicationServices,
                    };

                    var initialFilteredInvocation = filteredInvocation;

                    for (var i = routeHandlerFilters.Count - 1; i >= 0; i--)
                    {
                        var filterFactory = routeHandlerFilters[i];
                        filteredInvocation = filterFactory(context0, filteredInvocation);
                    }
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
                var handler = (System.Func<int?, Microsoft.AspNetCore.Http.IResult>)del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {
                    var routeHandlerFilters = builder.FilterFactories;
                    filteredInvocation = ic =>
                    {
                        return System.Threading.Tasks.ValueTask.FromResult<object>(handler(ic.GetArgument<int?>(0)));
                    };

                    var context0 = new EndpointFilterFactoryContext
                    {
                        MethodInfo = handler.Method,
                        ApplicationServices = builder.ApplicationServices,
                    };

                    var initialFilteredInvocation = filteredInvocation;

                    for (var i = routeHandlerFilters.Count - 1; i >= 0; i--)
                    {
                        var filterFactory = routeHandlerFilters[i];
                        filteredInvocation = filterFactory(context0, filteredInvocation);
                    }
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

            map[(@"C:\dev\git\uController\samples\Program.cs", 43)] = (del, builder) =>
            {
                var handler = (System.Func<string>)del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {
                    var routeHandlerFilters = builder.FilterFactories;
                    filteredInvocation = ic =>
                    {
                        return System.Threading.Tasks.ValueTask.FromResult<object>(handler());
                    };

                    var context0 = new EndpointFilterFactoryContext
                    {
                        MethodInfo = handler.Method,
                        ApplicationServices = builder.ApplicationServices,
                    };

                    var initialFilteredInvocation = filteredInvocation;

                    for (var i = routeHandlerFilters.Count - 1; i >= 0; i--)
                    {
                        var filterFactory = routeHandlerFilters[i];
                        filteredInvocation = filterFactory(context0, filteredInvocation);
                    }
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
            var factory = map[(filePath, lineNumber)];
            var conventionBuilder = routes.MapGet(pattern, (System.Delegate)handler);
            conventionBuilder.Finally(e =>
            {
                e.RequestDelegate = factory(handler, e);
            });

            return conventionBuilder;
        }

        internal static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapGet(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, string pattern, System.Func<string, string> handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            var factory = map[(filePath, lineNumber)];
            var conventionBuilder = routes.MapGet(pattern, (System.Delegate)handler);
            conventionBuilder.Finally(e =>
            {
                e.RequestDelegate = factory(handler, e);
            });

            return conventionBuilder;
        }

        internal static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapGet(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, string pattern, System.Func<Person> handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            var factory = map[(filePath, lineNumber)];
            var conventionBuilder = routes.MapGet(pattern, (System.Delegate)handler);
            conventionBuilder.Finally(e =>
            {
                e.RequestDelegate = factory(handler, e);
            });

            return conventionBuilder;
        }

        internal static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapGet(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, string pattern, System.Func<System.Security.Claims.ClaimsPrincipal, Microsoft.AspNetCore.Http.IResult> handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            var factory = map[(filePath, lineNumber)];
            var conventionBuilder = routes.MapGet(pattern, (System.Delegate)handler);
            conventionBuilder.Finally(e =>
            {
                e.RequestDelegate = factory(handler, e);
            });

            return conventionBuilder;
        }

        internal static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapPost(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, string pattern, System.Func<System.Text.Json.Nodes.JsonNode, System.Text.Json.Nodes.JsonNode> handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            var factory = map[(filePath, lineNumber)];
            var conventionBuilder = routes.MapPost(pattern, (System.Delegate)handler);
            conventionBuilder.Finally(e =>
            {
                e.RequestDelegate = factory(handler, e);
            });

            return conventionBuilder;
        }

        internal static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder Map(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, string pattern, System.Func<int?, Microsoft.AspNetCore.Http.IResult> handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            var factory = map[(filePath, lineNumber)];
            var conventionBuilder = routes.Map(pattern, (System.Delegate)handler);
            conventionBuilder.Finally(e =>
            {
                e.RequestDelegate = factory(handler, e);
            });

            return conventionBuilder;
        }
    }
}