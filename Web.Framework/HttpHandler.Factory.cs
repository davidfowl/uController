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
        private static readonly MethodInfo ExecuteAsyncMethodInfo = typeof(HttpHandler).GetMethod(nameof(ExecuteResultAsync), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo JsonDeserializeMethodInfo = typeof(HttpHandler).GetMethod(nameof(JsonDeserialize), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo ActivatorMethodInfo = typeof(HttpHandler).GetMethod(nameof(CreateInstance), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo GetRequiredServiceMethodInfo = typeof(HttpHandler).GetMethod(nameof(GetRequiredService), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo ConvertToTaskMethodInfo = typeof(HttpHandler).GetMethod(nameof(ConvertTask), BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly MemberInfo CompletedTaskMemberInfo = GetMemberInfo<Func<Task>>(() => Task.CompletedTask);

        public static Func<RequestDelegate, RequestDelegate> Build<THttpHandler>(Action<HttpModel> configure = null)
        {
            return Build(typeof(THttpHandler), configure);
        }

        public static Func<RequestDelegate, RequestDelegate> Build(Type handlerType, Action<HttpModel> configure = null)
        {
            return BuildWithoutCache(handlerType, configure);
        }

        private static Func<RequestDelegate, RequestDelegate> BuildWithoutCache(Type handlerType, Action<HttpModel> configure)
        {
            var model = HttpModel.FromType(handlerType);

            configure?.Invoke(model);

            var bindings = new List<Binding>();

            foreach (var method in model.Methods)
            {
                var needForm = false;
                var httpMethod = method.HttpMethod;
                var template = method.RouteTemplate;

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

                        // Convert<T>(handler.Method(..))
                        methodCall = Expression.Call(
                                           ConvertToTaskMethodInfo.MakeGenericMethod(typeArg),
                                           methodCall);
                    }

                    body = Expression.Call(ExecuteAsyncMethodInfo, methodCall, httpContextArg);
                }

                var lambda = Expression.Lambda<Func<HttpContext, RouteValueDictionary, RequestDelegate, Task>>(body, httpContextArg, routeValuesArg, nextArg);

                var routeTemplate = method.RouteTemplate;
                var matcher = routeTemplate == null ? null : new TemplateMatcher(routeTemplate, new RouteValueDictionary());

                bindings.Add(new Binding
                {
                    MethodInfo = method.MethodInfo,
                    Invoke = lambda.Compile(),
                    NeedForm = needForm,
                    HttpMethod = httpMethod,
                    Matcher = matcher
                });
            }

            return next =>
            {
                return async context =>
                {
                    if (TryMatch(context, bindings, out var binding, out var routeValues))
                    {
                        // Generating async code would just be insane so if the method needs the form populate it here
                        // so the within the method it's cached
                        if (binding.NeedForm)
                        {
                            await context.Request.ReadFormAsync();
                        }

                        await binding.Invoke(context, routeValues, next);
                        return;
                    }

                    await next(context);
                };
            };
        }

        private static bool TryMatch(HttpContext context, List<Binding> bindings, out Binding binding, out RouteValueDictionary routeValues)
        {
            // Scores
            // nothing = 1
            // method = 2
            // route = 3
            // method + route = 5
            routeValues = null;
            binding = null;

            var matchValues = new RouteValueDictionary();
            object match = null;
            var matchScore = 0;

            foreach (var b in bindings)
            {
                var score = 0;

                if (b.Matcher != null)
                {
                    // Clear the previous values (if any)
                    matchValues.Clear();

                    // If there's a template, it has to match the path
                    if (b.Matcher.TryMatch(context.Request.Path, matchValues))
                    {
                        if (b.HttpMethod != null)
                        {
                            // If there's a method, it has to match
                            if (string.Equals(context.Request.Method, b.HttpMethod, StringComparison.OrdinalIgnoreCase))
                            {
                                score = 5;
                            }
                        }
                        else
                        {
                            score = 3;
                        }
                    }
                }
                else if (b.HttpMethod != null)
                {
                    // If there's a method, it has to match
                    if (string.Equals(context.Request.Method, b.HttpMethod, StringComparison.OrdinalIgnoreCase))
                    {
                        score = 2;
                    }
                }
                else
                {
                    // No method, so this is a candidate (no method means wildcard)
                    score = 1;
                }

                if (score > matchScore)
                {
                    match = b;
                    matchScore = score;
                    // Copy the values here
                    routeValues = new RouteValueDictionary(matchValues);
                }
                else if (score > 0 && score == matchScore)
                {
                    switch (match)
                    {
                        case Binding previous:
                            {
                                match = new List<Binding>(bindings.Count)
                                {
                                    previous,
                                    b
                                };
                            }
                            break;
                        case List<Binding> candidates:
                            candidates.Add(b);
                            break;
                    }
                }
            }

            switch (match)
            {
                case Binding b:
                    binding = b;
                    break;
                case List<Binding> candidates:
                    throw new InvalidOperationException($"Ambiguous match found: \r\n{GetCandidiatesString(candidates)}");
            }

            return binding != null;
        }

        private static string GetCandidiatesString(List<Binding> candidates)
        {
            return string.Join("\n", candidates.Select(c => GetMethodInfoString(c.MethodInfo)));
        }

        private static string GetMethodInfoString(MethodInfo methodInfo)
        {
            return $"{methodInfo.Name}({string.Join(",", methodInfo.GetParameters().Select(p => p.ParameterType.Name))})";
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
            else
            {
                // Convert.ChangeType()
                expr = Expression.Call(ChangeTypeMethodInfo, valueArg, Expression.Constant(type));
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

        private static async Task<object> ConvertTask<T>(Task<T> task)
        {
            return await task;
        }

        private static async Task ExecuteResultAsync(object result, HttpContext httpContext)
        {
            switch (result)
            {
                case Task<object> task:
                    {
                        var val = await task;
                        // We normalize to Task<object> then we execute the actual result
                        await ExecuteResultAsync(val, httpContext);
                    }
                    break;
                case Task task:
                    await task;
                    break;
                case RequestDelegate val:
                    await val(httpContext);
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

            public Func<HttpContext, RouteValueDictionary, RequestDelegate, Task> Invoke { get; set; }

            public TemplateMatcher Matcher { get; set; }

            public string HttpMethod { get; set; }

            public bool NeedForm { get; set; }
        }
    }
}

