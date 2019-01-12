using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Web.Framework
{
    public partial class HttpHandler
    {
        private static readonly MethodInfo ChangeTypeMethodInfo = GetMethodInfo<Func<object, Type, object>>((value, type) => Convert.ChangeType(value, type));
        private static readonly MethodInfo ExecuteTaskOfTMethodInfo = typeof(HttpHandler).GetMethod(nameof(ExecuteTask), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo ExecuteAsyncMethodInfo = typeof(HttpHandler).GetMethod(nameof(ExecuteResultAsync), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo JsonDeserializeMethodInfo = typeof(HttpHandler).GetMethod(nameof(JsonDeserialize), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo ActivatorMethodInfo = typeof(HttpHandler).GetMethod(nameof(CreateInstance), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo GetRequiredServiceMethodInfo = typeof(HttpHandler).GetMethod(nameof(GetRequiredService), BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly MemberInfo CompletedTaskMemberInfo = GetMemberInfo<Func<Task>>(() => Task.CompletedTask);

        public static List<Endpoint> Build<THttpHandler>(Action<HttpModel> configure = null)
        {
            return Build(typeof(THttpHandler), configure);
        }

        public static List<Endpoint> Build(Type handlerType, Action<HttpModel> configure = null)
        {
            return BuildWithoutCache(handlerType, configure);
        }

        private static List<Endpoint> BuildWithoutCache(Type handlerType, Action<HttpModel> configure)
        {
            var model = HttpModel.FromType(handlerType);

            configure?.Invoke(model);

            var endpoints = new List<Endpoint>();

            foreach (var method in model.Methods.Where(m => m.RoutePattern != null))
            {
                var needForm = false;
                var httpMethod = method.HttpMethod;
                var template = method.RoutePattern;

                // Non void return type

                // Task Invoke(HttpContext httpContext, RouteValueDictionary routeValues, RequestDelegate next)
                // {
                //     // The type is activated via DI if it has args
                //     return ExecuteResult(new THttpHandler(...).Method(..), httpContext);
                // }

                // void return type

                // Task Invoke(HttpContext httpContext, RouteValueDictionary routeValues, RequestDelegate next)
                // {
                //     new THttpHandler(...).Method(..)
                //     return Task.CompletedTask;
                // }

                var httpContextArg = Expression.Parameter(typeof(HttpContext), "httpContext");
                var routeValuesArg = Expression.Parameter(typeof(RouteValueDictionary), "routeValues");
                var nextArg = Expression.Parameter(typeof(RequestDelegate), "next");
                var requestServicesExpr = Expression.Property(httpContextArg, nameof(HttpContext.RequestServices));

                // Fast path: We can skip the activator if there's only a default ctor with 0 args
                var ctors = handlerType.GetConstructors();

                Expression httpHandlerExpression = null;

                if (method.MethodInfo.IsStatic)
                {
                    // Do nothing
                }
                else if (ctors.Length == 1 && ctors[0].GetParameters().Length == 0)
                {
                    httpHandlerExpression = Expression.New(ctors[0]);
                }
                else
                {
                    // CreateInstance<THttpHandler>(context.RequestServices)
                    httpHandlerExpression = Expression.Call(ActivatorMethodInfo.MakeGenericMethod(handlerType), requestServicesExpr);
                }

                var args = new List<Expression>();

                var httpRequestExpr = Expression.Property(httpContextArg, nameof(HttpContext.Request));

                foreach (var parameter in method.Parameters)
                {
                    Expression paramterExpression = Expression.Default(parameter.ParameterType);

                    if (parameter.FromQuery != null)
                    {
                        var queryProperty = Expression.Property(httpRequestExpr, nameof(HttpRequest.Query));
                        paramterExpression = BindArgument(queryProperty, parameter, parameter.FromQuery);
                    }
                    else if (parameter.FromHeader != null)
                    {
                        var headersProperty = Expression.Property(httpRequestExpr, nameof(HttpRequest.Headers));
                        paramterExpression = BindArgument(headersProperty, parameter, parameter.FromHeader);
                    }
                    else if (parameter.FromRoute != null)
                    {
                        paramterExpression = BindArgument(routeValuesArg, parameter, parameter.FromRoute);
                    }
                    else if (parameter.FromCookie != null)
                    {
                        var cookiesProperty = Expression.Property(httpRequestExpr, nameof(HttpRequest.Cookies));
                        paramterExpression = BindArgument(cookiesProperty, parameter, parameter.FromCookie);
                    }
                    else if (parameter.FromServices)
                    {
                        paramterExpression = Expression.Call(GetRequiredServiceMethodInfo.MakeGenericMethod(parameter.ParameterType), requestServicesExpr);
                    }
                    else if (parameter.FromForm != null)
                    {
                        needForm = true;

                        var formProperty = Expression.Property(httpRequestExpr, nameof(HttpRequest.Form));
                        paramterExpression = BindArgument(formProperty, parameter, parameter.FromForm);
                    }
                    else if (parameter.FromBody)
                    {
                        var bodyProperty = Expression.Property(httpRequestExpr, nameof(HttpRequest.Body));
                        paramterExpression = BindBody(bodyProperty, parameter);
                    }
                    else
                    {
                        if (parameter.ParameterType == typeof(IFormCollection))
                        {
                            paramterExpression = Expression.Property(httpRequestExpr, nameof(HttpRequest.Form));
                        }
                        else if (parameter.ParameterType == typeof(HttpContext))
                        {
                            paramterExpression = httpContextArg;
                        }
                        else if (parameter.ParameterType == typeof(RequestDelegate))
                        {
                            paramterExpression = nextArg;
                        }
                        else if (parameter.ParameterType == typeof(IHeaderDictionary))
                        {
                            paramterExpression = Expression.Property(httpRequestExpr, nameof(HttpRequest.Headers));
                        }
                    }

                    args.Add(paramterExpression);
                }

                Expression body = null;

                if (method.ReturnType == typeof(void))
                {
                    var bodyExpressions = new List<Expression>
                    {
                        Expression.Call(httpHandlerExpression, method.MethodInfo, args),
                        Expression.Property(null, (PropertyInfo)CompletedTaskMemberInfo)
                    };

                    body = Expression.Block(bodyExpressions);
                }
                else
                {
                    var methodCall = Expression.Call(httpHandlerExpression, method.MethodInfo, args);

                    // Coerce Task<T> to Task<object>
                    if (method.ReturnType.IsGenericType &&
                        method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        var typeArg = method.ReturnType.GetGenericArguments()[0];

                        // ExecuteTask<T>(handler.Method(..), httpContext);
                        body = Expression.Call(
                                           ExecuteTaskOfTMethodInfo.MakeGenericMethod(typeArg),
                                           methodCall,
                                           httpContextArg);
                    }
                    else
                    {
                        // ExecuteResult(handler.Method(..), httpContext);
                        body = Expression.Call(ExecuteAsyncMethodInfo, methodCall, httpContextArg);
                    }
                }

                var lambda = Expression.Lambda<Func<HttpContext, RouteValueDictionary, RequestDelegate, Task>>(body, httpContextArg, routeValuesArg, nextArg);

                var routeTemplate = method.RoutePattern;

                var invoker = lambda.Compile();

                var routeEndpointModel = new RouteEndpointModel(
                    async httpContext =>
                    {
                        // Generating async code would just be insane so if the method needs the form populate it here
                        // so the within the method it's cached
                        if (needForm)
                        {
                            await httpContext.Request.ReadFormAsync();
                        }

                        await invoker.Invoke(httpContext, httpContext.Request.RouteValues, (c) => Task.CompletedTask);
                    },
                    routeTemplate,
                    0);
                routeEndpointModel.DisplayName = routeTemplate.RawText;

                if (!string.IsNullOrEmpty(method.HttpMethod))
                {
                    routeEndpointModel.Metadata.Add(new HttpMethodMetadata(new[] { method.HttpMethod }));
                }

                foreach (var attribute in method.MethodInfo.GetCustomAttributes(true))
                {
                    routeEndpointModel.Metadata.Add(attribute);
                }

                foreach (var convention in method.Conventions)
                {
                    convention(routeEndpointModel);
                }

                endpoints.Add(routeEndpointModel.Build());
            }

            return endpoints;
        }

        private static Expression BindBody(Expression httpBody, ParameterModel parameter)
        {
            // Hard coded to JSON (and JSON.NET at that!)
            // Also this is synchronous, good luck generating async anything
            // new JsonSerializer().Deserialize(
            //     new JsonTextReader(
            //         new HttpRequestStreamReader(
            //            context.Request.Body, Encoding.UTF8)), p.ParameterType);
            //
            var streamReaderCtor = typeof(HttpRequestStreamReader).GetConstructor(new[] { typeof(Stream), typeof(Encoding) });
            var streamReader = Expression.New(streamReaderCtor, httpBody, Expression.Constant(Encoding.UTF8));

            var textReaderCtor = typeof(JsonTextReader).GetConstructor(new[] { typeof(TextReader) });
            var textReader = Expression.New(textReaderCtor, streamReader);

            Expression expr = Expression.Call(JsonDeserializeMethodInfo.MakeGenericMethod(parameter.ParameterType), textReader);

            return expr;
        }

        private static Expression BindArgument(Expression sourceExpression, ParameterModel parameter, string name)
        {
            var key = name ?? parameter.Name;
            var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
            var valueArg = Expression.Convert(
                                Expression.MakeIndex(sourceExpression,
                                                     sourceExpression.Type.GetProperty("Item"),
                                                     new[] {
                                                         Expression.Constant(key)
                                                     }),
                                typeof(string));

            MethodInfo parseMethod = (from m in type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                      let parameters = m.GetParameters()
                                      where m.Name == "Parse" && parameters.Length == 1 && parameters[0].ParameterType == typeof(string)
                                      select m).FirstOrDefault();

            Expression expr = null;

            if (parseMethod != null)
            {
                expr = Expression.Call(parseMethod, valueArg);
            }
            else if (parameter.ParameterType != valueArg.Type)
            {
                // Convert.ChangeType()
                expr = Expression.Call(ChangeTypeMethodInfo, valueArg, Expression.Constant(type));
            }
            else
            {
                expr = valueArg;
            }

            if (expr.Type != parameter.ParameterType)
            {
                expr = Expression.Convert(expr, parameter.ParameterType);
            }

            // property[key] == null ? default : (ParameterType){Type}.Parse(property[key]);
            expr = Expression.Condition(
                Expression.Equal(valueArg, Expression.Constant(null)),
                Expression.Default(parameter.ParameterType),
                expr);

            return expr;
        }

        private static MethodInfo GetMethodInfo<T>(Expression<T> expr)
        {
            var mc = (MethodCallExpression)expr.Body;
            return mc.Method;
        }

        private static MemberInfo GetMemberInfo<T>(Expression<T> expr)
        {
            var mc = (MemberExpression)expr.Body;
            return mc.Member;
        }

        private static T GetRequiredService<T>(IServiceProvider sp)
        {
            return sp.GetRequiredService<T>();
        }

        private static T CreateInstance<T>(IServiceProvider sp)
        {
            return ActivatorUtilities.CreateInstance<T>(sp);
        }

        private static T JsonDeserialize<T>(JsonTextReader jsonReader)
        {
            return new JsonSerializer().Deserialize<T>(jsonReader);
        }

        private static async Task ExecuteTask<T>(Task<T> task, HttpContext httpContext)
        {
            var result = await task;
            await ExecuteResultAsync(result, httpContext);
        }

        private static async Task ExecuteResultAsync(object result, HttpContext httpContext)
        {
            switch (result)
            {
                case Task task:
                    await task;
                    break;
                case RequestDelegate val:
                    await val(httpContext);
                    break;
                case null:
                    httpContext.Response.StatusCode = 404;
                    break;
                default:
                    {
                        var val = new JsonResult(result);
                        await val.ExecuteAsync(httpContext);
                    }
                    break;
            }
        }

        private class Binding
        {
            public MethodInfo MethodInfo { get; set; }

            public Expression DebugExpression { get; set; }

            public Func<HttpContext, RouteValueDictionary, RequestDelegate, Task> Invoke { get; set; }

            public TemplateMatcher Matcher { get; set; }

            public string HttpMethod { get; set; }

            public bool NeedForm { get; set; }
        }
    }
}

