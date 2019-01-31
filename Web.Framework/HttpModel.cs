using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Web.Framework
{
    public class HttpModel
    {
        public List<MethodModel> Methods { get; } = new List<MethodModel>();

        public static HttpModel FromType(Type type)
        {
            var model = new HttpModel();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

            var routeAttribute = type.GetCustomAttribute<RouteAttribute>();

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<HttpMethodAttribute>();
                var template = CombineRoute(routeAttribute?.Template, attribute?.Template ?? method.GetCustomAttribute<RouteAttribute>()?.Template);

                var methodModel = new MethodModel
                {
                    MethodInfo = method,
                    RoutePattern = template == null ? null : RoutePatternFactory.Parse(template)
                };

                // Add all attributes as metadata
                foreach (var metadata in method.GetCustomAttributes(inherit: true))
                {
                    methodModel.Metadata.Add(metadata);
                }

                foreach (var parameter in method.GetParameters())
                {
                    var fromQuery = parameter.GetCustomAttribute<FromQueryAttribute>();
                    var fromHeader = parameter.GetCustomAttribute<FromHeaderAttribute>();
                    var fromForm = parameter.GetCustomAttribute<FromFormAttribute>();
                    var fromBody = parameter.GetCustomAttribute<FromBodyAttribute>();
                    var fromRoute = parameter.GetCustomAttribute<FromRouteAttribute>();
                    var fromCookie = parameter.GetCustomAttribute<FromCookieAttribute>();
                    var fromService = parameter.GetCustomAttribute<FromServicesAttribute>();

                    methodModel.Parameters.Add(new ParameterModel
                    {
                        Name = parameter.Name,
                        ParameterType = parameter.ParameterType,
                        FromQuery = fromQuery == null ? null : fromQuery?.Name ?? parameter.Name,
                        FromHeader = fromHeader == null ? null : fromHeader?.Name ?? parameter.Name,
                        FromForm = fromForm == null ? null : fromForm?.Name ?? parameter.Name,
                        FromRoute = fromRoute == null ? null : fromRoute?.Name ?? parameter.Name,
                        FromCookie = fromCookie == null ? null : fromCookie?.Name,
                        FromBody = fromBody != null,
                        FromServices = fromService != null
                    });
                }

                model.Methods.Add(methodModel);
            }

            return model;
        }

        private static string CombineRoute(string prefix, string template)
        {
            if (prefix == null)
            {
                return template;
            }

            if (template == null)
            {
                return prefix;
            }

            return prefix + '/' + template.TrimStart('/');
        }
    }

    public class MethodModel
    {
        public MethodInfo MethodInfo { get; set; }
        public List<ParameterModel> Parameters { get; } = new List<ParameterModel>();
        public List<object> Metadata { get; } = new List<object>();
        public RoutePattern RoutePattern { get; set; }
    }

    public class ParameterModel
    {
        public string Name { get; set; }
        public Type ParameterType { get; set; }
        public string FromQuery { get; set; }
        public string FromHeader { get; set; }
        public string FromForm { get; set; }
        public string FromRoute { get; set; }
        public string FromCookie { get; set; }
        public bool FromBody { get; set; }
        public bool FromServices { get; set; }

        public bool HasBindingSource => FromBody || FromServices || FromCookie != null ||
            FromForm != null || FromQuery != null || FromHeader != null || FromRoute != null;
    }
}
