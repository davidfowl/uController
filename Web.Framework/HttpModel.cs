using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Web.Framework
{
    public class HttpModel
    {
        public List<ActionModel> Actions { get; } = new List<ActionModel>();

        public static HttpModel FromType(Type type)
        {
            var model = new HttpModel();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<HttpMethodAttribute>();
                var httpMethod = attribute?.Method ?? "";

                var action = new ActionModel
                {
                    MethodInfo = method,
                    ReturnType = method.ReturnType,
                    HttpMethod = httpMethod,
                    Template = attribute?.Template
                };

                foreach (var p in method.GetParameters())
                {
                    var fromQuery = p.GetCustomAttribute<FromQueryAttribute>();
                    var fromHeader = p.GetCustomAttribute<FromHeaderAttribute>();
                    var fromForm = p.GetCustomAttribute<FromFormAttribute>();
                    var fromBody = p.GetCustomAttribute<FromBodyAttribute>();
                    var fromRoute = p.GetCustomAttribute<FromRouteAttribute>();
                    var fromCookie = p.GetCustomAttribute<FromCookieAttribute>();
                    var fromService = p.GetCustomAttribute<FromServicesAttribute>();

                    action.Parameters.Add(new ParameterModel
                    {
                        Name = p.Name,
                        ParameterType = p.ParameterType,
                        FromQuery = fromQuery == null ? null : fromQuery?.Name ?? p.Name,
                        FromHeader = fromHeader == null ? null : fromHeader?.Name ?? p.Name,
                        FromForm = fromForm == null ? null : fromForm?.Name ?? p.Name,
                        FromRoute = fromRoute == null ? null : fromRoute?.Name ?? p.Name,
                        FromCookie = fromCookie == null ? null : fromCookie?.Name,
                        FromBody = fromBody != null,
                        FromServices = fromService != null
                    });
                }

                model.Actions.Add(action);
            }

            return model;
        }
    }

    public class ActionModel
    {
        public MethodInfo MethodInfo { get; set; }
        public List<ParameterModel> Parameters { get; } = new List<ParameterModel>();
        public Type ReturnType { get; set; }
        public string HttpMethod { get; set; }
        public string Template { get; set; }
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
    }
}
