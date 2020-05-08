using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace uController
{
    internal class HttpHandlerBuilder
    {
        private static readonly MethodInfo ChangeTypeMethodInfo = GetMethodInfo<Func<object, Type, object>>((value, type) => Convert.ChangeType(value, type));
        private static readonly MethodInfo ExecuteTaskOfTMethodInfo = typeof(HttpHandlerBuilder).GetMethod(nameof(ExecuteTask), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo ExecuteValueTaskOfTMethodInfo = typeof(HttpHandlerBuilder).GetMethod(nameof(ExecuteValueTask), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo ExecuteTaskResultOfTMethodInfo = typeof(HttpHandlerBuilder).GetMethod(nameof(ExecuteTaskResult), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo ExecuteValueResultTaskOfTMethodInfo = typeof(HttpHandlerBuilder).GetMethod(nameof(ExecuteValueTaskResult), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo GetRequiredServiceMethodInfo = typeof(HttpHandlerBuilder).GetMethod(nameof(GetRequiredService), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo ObjectResultExecuteAsync = typeof(ObjectResult).GetMethod(nameof(ObjectResult.ExecuteAsync), BindingFlags.Public | BindingFlags.Instance);
        private static readonly MethodInfo ResultExecuteAsync = typeof(Result).GetMethod(nameof(Result.ExecuteAsync), BindingFlags.Public | BindingFlags.Instance);

        private static readonly MemberInfo CompletedTaskMemberInfo = GetMemberInfo<Func<Task>>(() => Task.CompletedTask);

        private static readonly ConstructorInfo ObjectResultCtor = typeof(ObjectResult).GetConstructors()[0];

        internal static void Build<THttpHandler>(IEndpointRouteBuilder routes)
        {
            Build(typeof(THttpHandler), routes);
        }

        // Expression tree impl
        internal static void Build(Type handlerType, IEndpointRouteBuilder routes)
        {
            var model = HttpModel.FromType(handlerType);

            ObjectFactory factory = null;

            // REVIEW: Should this be lazy?
            var httpRequestReader = routes.ServiceProvider.GetRequiredService<IHttpRequestReader>();

            foreach (var method in model.Methods)
            {
                // Nothing to route to
                if (method.RoutePattern == null)
                {
                    continue;
                }

                var needForm = false;
                var needBody = false;
                Type bodyType = null;
                // Non void return type

                // Task Invoke(HttpContext httpContext)
                // {
                //     // The type is activated via DI if it has args
                //     return ExecuteResultAsync(new THttpHandler(...).Method(..), httpContext);
                // }

                // void return type

                // Task Invoke(HttpContext httpContext)
                // {
                //     new THttpHandler(...).Method(..)
                //     return Task.CompletedTask;
                // }

                var httpContextArg = Expression.Parameter(typeof(HttpContext), "httpContext");
                // This argument represents the deserialized body returned from IHttpRequestReader
                // when the method has a FromBody attribute declared
                var deserializedBodyArg = Expression.Parameter(typeof(object), "bodyValue");

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
                    // Create a factory lazily for this handlerType
                    if (factory == null)
                    {
                        factory = ActivatorUtilities.CreateFactory(handlerType, Type.EmptyTypes);
                    }

                    // This invokes the cached factory to create the instance then casts it to the target type
                    var invokeFactoryExpr = Expression.Invoke(Expression.Constant(factory), requestServicesExpr, Expression.Constant(null, typeof(object[])));
                    httpHandlerExpression = Expression.Convert(invokeFactoryExpr, handlerType);
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
                        var routeValuesProperty = Expression.Property(httpRequestExpr, nameof(HttpRequest.RouteValues));
                        paramterExpression = BindArgument(routeValuesProperty, parameter, parameter.FromRoute);
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
                        if (needBody)
                        {
                            throw new InvalidOperationException(method.MethodInfo.Name + " cannot have more than one FromBody attribute.");
                        }

                        if (needForm)
                        {
                            throw new InvalidOperationException(method.MethodInfo.Name + " cannot mix FromBody and FromForm on the same method.");
                        }

                        needBody = true;
                        bodyType = parameter.ParameterType;
                        paramterExpression = Expression.Convert(deserializedBodyArg, bodyType);
                    }
                    else
                    {
                        if (parameter.ParameterType == typeof(IFormCollection))
                        {
                            needForm = true;

                            paramterExpression = Expression.Property(httpRequestExpr, nameof(HttpRequest.Form));
                        }
                        else if (parameter.ParameterType == typeof(HttpContext))
                        {
                            paramterExpression = httpContextArg;
                        }
                    }

                    args.Add(paramterExpression);
                }

                Expression body = null;

                var methodCall = Expression.Call(httpHandlerExpression, method.MethodInfo, args);

                // Exact request delegate match
                if (method.MethodInfo.ReturnType == typeof(void))
                {
                    var bodyExpressions = new List<Expression>
                    {
                        methodCall,
                        Expression.Property(null, (PropertyInfo)CompletedTaskMemberInfo)
                    };

                    body = Expression.Block(bodyExpressions);
                }
                else if (AwaitableInfo.IsTypeAwaitable(method.MethodInfo.ReturnType, out var info))
                {
                    if (method.MethodInfo.ReturnType == typeof(Task))
                    {
                        body = methodCall;
                    }
                    else if (method.MethodInfo.ReturnType.IsGenericType &&
                             method.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        var typeArg = method.MethodInfo.ReturnType.GetGenericArguments()[0];

                        if (typeof(Result).IsAssignableFrom(typeArg))
                        {
                            body = Expression.Call(
                                               ExecuteTaskResultOfTMethodInfo.MakeGenericMethod(typeArg),
                                               methodCall,
                                               httpContextArg);
                        }
                        else
                        {
                            // ExecuteTask<T>(handler.Method(..), httpContext);
                            body = Expression.Call(
                                               ExecuteTaskOfTMethodInfo.MakeGenericMethod(typeArg),
                                               methodCall,
                                               httpContextArg);
                        }
                    }
                    else if (method.MethodInfo.ReturnType.IsGenericType &&
                             method.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                    {
                        var typeArg = method.MethodInfo.ReturnType.GetGenericArguments()[0];

                        if (typeof(Result).IsAssignableFrom(typeArg))
                        {
                            body = Expression.Call(
                                               ExecuteValueResultTaskOfTMethodInfo.MakeGenericMethod(typeArg),
                                               methodCall,
                                               httpContextArg);
                        }
                        else
                        {
                            // ExecuteTask<T>(handler.Method(..), httpContext);
                            body = Expression.Call(
                                           ExecuteValueTaskOfTMethodInfo.MakeGenericMethod(typeArg),
                                           methodCall,
                                           httpContextArg);
                        }
                    }
                    else
                    {
                        // TODO: Handle custom awaitables
                        throw new NotSupportedException("Unsupported return type " + method.MethodInfo.ReturnType);
                    }
                }
                else if (typeof(Result).IsAssignableFrom(method.MethodInfo.ReturnType))
                {
                    body = Expression.Call(methodCall, ResultExecuteAsync, httpContextArg);
                }
                else
                {
                    var newObjectResult = Expression.New(ObjectResultCtor, methodCall);
                    body = Expression.Call(newObjectResult, ObjectResultExecuteAsync, httpContextArg);
                }

                RequestDelegate requestDelegate = null;

                if (needBody)
                {
                    // We need to generate the code for reading from the body before calling into the 
                    // delegate
                    var lambda = Expression.Lambda<Func<HttpContext, object, Task>>(body, httpContextArg, deserializedBodyArg);
                    var invoker = lambda.Compile();

                    requestDelegate = async httpContext =>
                    {
                        var bodyValue = await httpRequestReader.ReadAsync(httpContext, bodyType);

                        await invoker(httpContext, bodyValue);
                    };
                }
                else if (needForm)
                {
                    var lambda = Expression.Lambda<RequestDelegate>(body, httpContextArg);
                    var invoker = lambda.Compile();

                    requestDelegate = async httpContext =>
                    {
                        // Generating async code would just be insane so if the method needs the form populate it here
                        // so the within the method it's cached
                        await httpContext.Request.ReadFormAsync();

                        await invoker(httpContext);
                    };
                }
                else
                {
                    var lambda = Expression.Lambda<RequestDelegate>(body, httpContextArg);
                    var invoker = lambda.Compile();

                    requestDelegate = invoker;
                }

                var displayName = method.MethodInfo.DeclaringType.Name + "." + method.MethodInfo.Name;

                routes.Map(method.RoutePattern, requestDelegate).Add(b =>
                {
                    foreach (CustomAttributeData item in method.Metadata)
                    {
                        var attr = item.Constructor.Invoke(item.ConstructorArguments.Select(a => a.Value).ToArray());
                        b.Metadata.Add(attr);
                    }
                });
            }
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

        private static async Task ExecuteTask<T>(Task<T> task, HttpContext httpContext)
        {
            await new ObjectResult(await task).ExecuteAsync(httpContext);
        }

        private static Task ExecuteValueTask<T>(ValueTask<T> task, HttpContext httpContext)
        {
            static async Task ExecuteAwaited(ValueTask<T> task, HttpContext httpContext)
            {
                await new ObjectResult(await task).ExecuteAsync(httpContext);
            }

            if (task.IsCompletedSuccessfully)
            {
                return new ObjectResult(task.GetAwaiter().GetResult()).ExecuteAsync(httpContext);
            }

            return ExecuteAwaited(task, httpContext);
        }

        private static Task ExecuteValueTaskResult<T>(ValueTask<T> task, HttpContext httpContext) where T : Result
        {
            static async Task ExecuteAwaited(ValueTask<T> task, HttpContext httpContext)
            {
                await (await task).ExecuteAsync(httpContext);
            }

            if (task.IsCompletedSuccessfully)
            {
                return task.GetAwaiter().GetResult().ExecuteAsync(httpContext);
            }

            return ExecuteAwaited(task, httpContext);
        }

        private static async Task ExecuteTaskResult<T>(Task<T> task, HttpContext httpContext) where T : Result
        {
            await (await task).ExecuteAsync(httpContext);
        }
    }
}
