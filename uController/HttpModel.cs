using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Routing.Patterns;

namespace uController
{
    public class HttpModel
    {
        public HttpModel(Type handlerType)
        {
            HandlerType = handlerType;
        }

        public List<MethodModel> Methods { get; } = new List<MethodModel>();

        public Type HandlerType { get; }

        public static HttpModel FromType(Type type)
        {
            var model = new HttpModel(type);

            var routeAttributeType = type.BaseType.Assembly.GetType(typeof(RouteAttribute).FullName);
            var httpMethodAttributeType = type.BaseType.Assembly.GetType(typeof(HttpMethodAttribute).FullName);
            var fromQueryAttributeType = type.BaseType.Assembly.GetType(typeof(FromQueryAttribute).FullName);
            var fromHeaderAttributeType = type.BaseType.Assembly.GetType(typeof(FromHeaderAttribute).FullName);
            var fromFormAttributeType = type.BaseType.Assembly.GetType(typeof(FromFormAttribute).FullName);
            var fromBodyAttributeType = type.BaseType.Assembly.GetType(typeof(FromBodyAttribute).FullName);
            var fromRouteAttributeType = type.BaseType.Assembly.GetType(typeof(FromRouteAttribute).FullName);
            var fromCookieAttributeType = type.BaseType.Assembly.GetType(typeof(FromCookieAttribute).FullName);
            var fromServicesAttributeType = type.BaseType.Assembly.GetType(typeof(FromServicesAttribute).FullName);

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

            var routeAttribute = type.GetCustomAttributeData(routeAttributeType);

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttributeData(httpMethodAttributeType);
                var template = CombineRoute(routeAttribute?.GetConstructorArgument<string>(0), attribute?.GetConstructorArgument<string>(0) ?? method.GetCustomAttributeData(routeAttributeType)?.GetConstructorArgument<string>(0));

                var methodModel = new MethodModel
                {
                    MethodInfo = method,
                    RoutePattern = template == null ? null : RoutePatternFactory.Parse(template)
                };

                // Add all attributes as metadata
                //foreach (var metadata in method.GetCustomAttributes(inherit: true))
                //{
                //    methodModel.Metadata.Add(metadata);
                //}
                foreach (var metadata in method.CustomAttributes)
                {
                    if (metadata.AttributeType.Namespace == "System.Runtime.CompilerServices" ||
                        metadata.AttributeType.Name == "DebuggerStepThroughAttribute")
                    {
                        continue;
                    }
                    methodModel.Metadata.Add(metadata);
                }

                foreach (var parameter in method.GetParameters())
                {
                    var fromQuery = parameter.GetCustomAttributeData(fromQueryAttributeType);
                    var fromHeader = parameter.GetCustomAttributeData(fromHeaderAttributeType);
                    var fromForm = parameter.GetCustomAttributeData(fromFormAttributeType);
                    var fromBody = parameter.GetCustomAttributeData(fromBodyAttributeType);
                    var fromRoute = parameter.GetCustomAttributeData(fromRouteAttributeType);
                    var fromCookie = parameter.GetCustomAttributeData(fromCookieAttributeType);
                    var fromService = parameter.GetCustomAttributeData(fromServicesAttributeType);

                    methodModel.Parameters.Add(new ParameterModel
                    {
                        Name = parameter.Name,
                        ParameterType = parameter.ParameterType,
                        FromQuery = fromQuery == null ? null : fromQuery?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromHeader = fromHeader == null ? null : fromHeader?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromForm = fromForm == null ? null : fromForm?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromRoute = fromRoute == null ? null : fromRoute?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromCookie = fromCookie == null ? null : fromCookie?.GetConstructorArgument<string>(0),
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
