using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Web.Framework
{
    // Fluent API for modifying the http model
    public static class HttpModelExtensions
    {
        public static MethodModel Method(this HttpModel model, string method)
        {
            return model.Methods.First(m => m.MethodInfo.Name == method);
        }

        public static MethodModel Route(this MethodModel model, string template)
        {
            template = template ?? throw new ArgumentNullException(nameof(template));
            model.RoutePattern = template == null ? null : RoutePatternFactory.Parse(template);
            return model;
        }

        public static MethodModel Get(this MethodModel model, string template = null)
        {
            model.HttpMethod = HttpMethods.Get;

            if (template == null)
            {
                return model;
            }

            return model.Route(template);
        }

        public static MethodModel Post(this MethodModel model, string template = null)
        {
            model.HttpMethod = HttpMethods.Post;

            if (template == null)
            {
                return model;
            }

            return model.Route(template);
        }

        public static MethodModel Put(this MethodModel model, string template = null)
        {
            model.HttpMethod = HttpMethods.Put;

            if (template == null)
            {
                return model;
            }

            return model.Route(template);
        }

        public static MethodModel Delete(this MethodModel model, string template = null)
        {
            model.HttpMethod = HttpMethods.Delete;

            if (template == null)
            {
                return model;
            }

            return model.Route(template);
        }

        public static ParameterModel Parameter(this MethodModel model, string parameter)
        {
            return model.Parameters.First(p => p.Name == parameter);
        }

        public static MethodModel FromBody(this MethodModel model, string parameter)
        {
            model.Parameter(parameter).FromBody = true;
            return model;
        }

        public static MethodModel FromQuery(this MethodModel model, string parameter, string name = null)
        {
            model.Parameter(parameter).FromQuery = name ?? parameter;
            return model;
        }

        public static MethodModel FromRoute(this MethodModel model, string parameter, string name = null)
        {
            model.Parameter(parameter).FromRoute = name ?? parameter;
            return model;
        }

        public static MethodModel FromHeader(this MethodModel model, string parameter, string name = null)
        {
            model.Parameter(parameter).FromHeader = name ?? parameter;
            return model;
        }
    }
}
