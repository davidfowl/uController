using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Web.Framework;

namespace Samples
{
    public static class HttpAppModelExtensions
    {
        public static HttpModel MapComplexTypeArgsToFromBody(this HttpModel model)
        {
            foreach (var m in model.Methods)
            {
                if (HttpMethods.IsPost(m.HttpMethod) || HttpMethods.IsPut(m.HttpMethod) || m.HttpMethod == null)
                {
                    foreach (var p in m.Parameters)
                    {
                        if (p.HasBindingSource)
                        {
                            continue;
                        }
                        // This is an MVC heuristic
                        p.FromBody = !TypeDescriptor.GetConverter(p.ParameterType).CanConvertFrom(typeof(string));
                    }
                }
            }
            return model;
        }

        public static HttpModel MapMethodNamesToHttpMethods(this HttpModel model)
        {
            foreach (var m in model.Methods)
            {
                if (m.MethodInfo.Name.StartsWith("Get", StringComparison.OrdinalIgnoreCase))
                {
                    m.Get();
                }
                else if (m.MethodInfo.Name.StartsWith("Post", StringComparison.OrdinalIgnoreCase))
                {
                    m.Post();
                }
                else if (m.MethodInfo.Name.StartsWith("Put", StringComparison.OrdinalIgnoreCase))
                {
                    m.Put();
                }
                else if (m.MethodInfo.Name.StartsWith("Delete", StringComparison.OrdinalIgnoreCase))
                {
                    m.Delete();
                }
            }

            return model;
        }

        public static HttpModel MapRouteParametersToMethodArguments(this HttpModel model)
        {
            foreach (var m in model.Methods)
            {
                m.MapRouteParameters();
            }
            return model;
        }

        public static MethodModel MapRouteParameters(this MethodModel model)
        {
            if (model.RoutePattern == null)
            {
                // No route, nothing to do here
                return model;
            }

            foreach (var p in model.Parameters)
            {
                if (p.HasBindingSource)
                {
                    // Something set, don't override
                    continue;
                }

                var part = model.RoutePattern.GetParameter(p.Name);
                if (part != null)
                {
                    p.FromRoute = part.Name;
                }
            }

            return model;
        }
    }
}
