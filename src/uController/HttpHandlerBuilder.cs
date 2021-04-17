using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace uController
{
    internal class HttpHandlerBuilder
    {
        internal static void Build<THttpHandler>(IEndpointRouteBuilder routes)
        {
            Build(typeof(THttpHandler), routes);
        }

        internal static void Build(Type handlerType, IEndpointRouteBuilder routes)
        {
            var model = HttpModel.FromType(handlerType, typeof(ObjectResult).Assembly);

            ObjectFactory factory = null;

            foreach (var method in model.Methods)
            {
                // Nothing to route to
                if (method.RoutePattern is null)
                {
                    continue;
                }

                var displayName = method.MethodInfo.DeclaringType.Name + "." + method.MethodInfo.Name;

                RequestDelegate requestDelegate = null;

                if (method.MethodInfo.IsStatic)
                {
                    requestDelegate = RequestDelegateFactory.Create(method.MethodInfo);
                }
                else
                {
                    if (factory == null)
                    {
                        factory = ActivatorUtilities.CreateFactory(handlerType, Type.EmptyTypes);
                    }

                    requestDelegate = RequestDelegateFactory.Create(method.MethodInfo, context => factory(context.RequestServices, null));
                }

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
    }
}
