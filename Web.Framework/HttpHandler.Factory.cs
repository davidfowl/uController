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
        private static readonly MethodInfo ExecuteAsyncMethodInfo = GetMethodInfo<Func<object, HttpContext, Task>>((result, httpContext) => ExecuteResultAsync(result, httpContext));
        private static readonly MethodInfo ChangeTypeMethodInfo = GetMethodInfo<Func<object, Type, object>>((value, type) => Convert.ChangeType(value, type));
        private static readonly MethodInfo JsonDeserializeMethodInfo = GetMethodInfo<Func<JsonTextReader, Type, object>>((jsonReader, type) => JsonDeserialize(jsonReader, type));
        private static readonly MethodInfo ActivatorMethodInfo = GetMethodInfo<Func<IServiceProvider, Type, object>>((sp, type) => CreateInstance(sp, type));
        private static readonly MethodInfo GetRequiredServiceMethodInfo = GetMethodInfo<Func<IServiceProvider, Type, object>>((sp, type) => sp.GetRequiredService(type));
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
            var routeKey = new object();

            foreach (var action in model.Actions)
            {
                var needForm = false;
                var httpMethod = action.HttpMethod;
                var template = action.Template;

                // Non void return type

                // Task Invoke(HttpContext context, RequestDelegate next)
                // {
                //     // The type is activated via DI if it has args
                //     return ExecuteResult(new THttpHandler(...).Method(..), httpContext);
                // }

                // void return type

                // Task Invoke(HttpContext context, RequestDelegate next)
                // {
                //     new THttpHandler(...).Method(..)
                //     return Task.CompletedTask;
                // }

                var httpContextArg = Expression.Parameter(typeof(HttpContext), "httpContext");
                var nextArg = Expression.Parameter(typeof(RequestDelegate), "next");
                var requestServicesExpr = Expression.Property(httpContextArg, nameof(HttpContext.RequestServices));

                // Fast path: We can skip the activator if there's only a default ctor with 0 args
                var ctors = handlerType.GetConstructors();

                Expression httpHandlerExpression = null;
                if (ctors.Length == 1 && ctors[0].GetParameters().Length == 0)
                {
                    httpHandlerExpression = Expression.New(ctors[0]);
                }
                else
                {
                    // (THttpHandler)ActivatorUtilities.CreateInstance(
                    //            context.RequestServices, 
                    //            typeof(THttpHandler));
                    httpHandlerExpression = Expression.Convert(
                                                Expression.Call(ActivatorMethodInfo,
                                                                requestServicesExpr,
                                                                Expression.Constant(handlerType)),
                                                handlerType);
                }

                var args = new List<Expression>();

                var httpRequestExpr = Expression.Property(httpContextArg, nameof(HttpContext.Request));

                foreach (var p in action.Parameters)
                {
                    Expression paramterExpression = Expression.Default(p.ParameterType);

                    if (p.FromQuery != null)
                    {
                        var queryProperty = Expression.Property(httpRequestExpr, nameof(HttpRequest.Query));
                        paramterExpression = BindArgument(queryProperty, p, p.FromQuery);
                    }
                    else if (p.FromHeader != null)
                    {
                        var headersProperty = Expression.Property(httpRequestExpr, nameof(HttpRequest.Headers));
                        paramterExpression = BindArgument(headersProperty, p, p.FromHeader);
                    }
                    else if (p.FromRoute != null)
                    {
                        var itemsProperty = Expression.Property(httpContextArg, nameof(HttpContext.Items));
                        var routeValuesVar = Expression.Convert(
                                                Expression.MakeIndex(itemsProperty,
                                                                     itemsProperty.Type.GetProperty("Item"),
                                                                     new[] {
                                                                         Expression.Constant(routeKey)
                                                                     }),
                                                typeof(RouteValueDictionary));

                        paramterExpression = BindArgument(routeValuesVar, p, p.FromRoute);
                    }
                    else if (p.FromCookie != null)
                    {
                        var cookiesProperty = Expression.Property(httpRequestExpr, nameof(HttpRequest.Cookies));
                        paramterExpression = BindArgument(cookiesProperty, p, p.FromCookie);
                    }
                    else if (p.FromServices)
                    {
                        paramterExpression = Expression.Convert(
                             Expression.Call(GetRequiredServiceMethodInfo,
                                             requestServicesExpr,
                                             Expression.Constant(p.ParameterType)),
                             p.ParameterType);
                    }
                    else if (p.FromForm != null)
                    {
                        needForm = true;

                        var formProperty = Expression.Property(httpRequestExpr, nameof(HttpRequest.Form));
                        paramterExpression = BindArgument(formProperty, p, p.FromForm);
                    }
                    else if (p.FromBody)
                    {
                        var bodyProperty = Expression.Property(httpRequestExpr, nameof(HttpRequest.Body));
                        paramterExpression = BindBody(bodyProperty, p);
                    }
                    else
                    {
                        if (p.ParameterType == typeof(IFormCollection))
                        {
                            paramterExpression = Expression.Property(httpRequestExpr, nameof(HttpRequest.Form));
                        }
                        else if (p.ParameterType == typeof(HttpContext))
                        {
                            paramterExpression = httpContextArg;
                        }
                        else if (p.ParameterType == typeof(RequestDelegate))
                        {
                            paramterExpression = nextArg;
                        }
                        else if (p.ParameterType == typeof(IHeaderDictionary))
                        {
                            paramterExpression = Expression.Property(httpRequestExpr, nameof(HttpRequest.Headers));
                        }
                    }

                    args.Add(paramterExpression);
                }

                Expression body = null;

                if (action.ReturnType == typeof(void))
                {
                    var bodyExpressions = new List<Expression>
                    {
                        Expression.Call(httpHandlerExpression, action.MethodInfo, args),
                        Expression.Property(null, (PropertyInfo)CompletedTaskMemberInfo)
                    };

                    body = Expression.Block(bodyExpressions);
                }
                else
                {
                    var methodCall = Expression.Call(httpHandlerExpression, action.MethodInfo, args);

                    // Coerce Task<T> to Task<object>
                    if (action.ReturnType.IsGenericType &&
                        action.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        var typeArg = action.ReturnType.GetGenericArguments()[0];

                        // Convert<T>(handler.Method(..))
                        methodCall = Expression.Call(
                                           ConvertToTaskMethodInfo.MakeGenericMethod(typeArg),
                                           methodCall);
                    }

                    body = Expression.Call(ExecuteAsyncMethodInfo, methodCall, httpContextArg);
                }

                var lambda = Expression.Lambda<Func<HttpContext, RequestDelegate, Task>>(body, httpContextArg, nextArg);

                bindings.Add(new Binding
                {
                    Invoke = lambda.Compile(),
                    NeedForm = needForm,
                    HttpMethod = httpMethod,
                    Template = template == null ? null : TemplateParser.Parse(template.TrimStart('~', '/')) // REVIEW: Trimming ~ and /, is that right?
                });
            }

            return next =>
            {
                return async context =>
                {
                    var binding = Match(context, bindings, out var routeValues);

                    if (binding != null)
                    {
                        // This is routing without routing
                        context.Items[routeKey] = routeValues;

                        // Generating async code would just be insane so if the method needs the form populate it here
                        // so the within the method it's cached
                        if (binding.NeedForm)
                        {
                            await context.Request.ReadFormAsync();
                        }

                        await binding.Invoke(context, next);
                        return;
                    }

                    await next(context);
                };
            };
        }

        private static Binding Match(HttpContext context, List<Binding> bindings, out RouteValueDictionary routeValues)
        {
            Binding binding = null;
            routeValues = null;
            var currentMaxScore = 0;

            foreach (var b in bindings)
            {
                int score = 0;
                if (string.Equals(context.Request.Method, b.HttpMethod, StringComparison.OrdinalIgnoreCase))
                {
                    score++;
                }

                var defaults = new RouteValueDictionary();
                var matchValues = new RouteValueDictionary();

                if (b.Template != null && new TemplateMatcher(b.Template, defaults).TryMatch(context.Request.Path, matchValues))
                {
                    score++;
                }

                if (score > currentMaxScore || binding == null)
                {
                    currentMaxScore = score;
                    binding = b;
                    routeValues = matchValues;
                }
            }

            return binding;
        }

        private static Expression BindBody(Expression httpBody, ParameterModel p)
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

            Expression expr = Expression.Call(JsonDeserializeMethodInfo, textReader, Expression.Constant(p.ParameterType));
            expr = Expression.Convert(expr, p.ParameterType);

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

        private static object CreateInstance(IServiceProvider sp, Type type)
        {
            return ActivatorUtilities.CreateInstance(sp, type);
        }

        private static object JsonDeserialize(JsonTextReader jsonReader, Type type)
        {
            return new JsonSerializer().Deserialize(jsonReader, type);
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
            public Func<HttpContext, RequestDelegate, Task> Invoke { get; set; }

            public RouteTemplate Template { get; set; }

            public string HttpMethod { get; set; }

            public bool NeedForm { get; set; }
        }
    }
}

