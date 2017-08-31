using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;

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
            model.Template = template;
            return model;
        }

        public static MethodModel Get(this MethodModel model, string template = null)
        {
            model.HttpMethod = HttpMethods.Get;
            model.Template = template;
            return model;
        }

        public static MethodModel Post(this MethodModel model, string template = null)
        {
            model.HttpMethod = HttpMethods.Post;
            model.Template = template;
            return model;
        }

        public static MethodModel Put(this MethodModel model, string template = null)
        {
            model.HttpMethod = HttpMethods.Put;
            model.Template = template;
            return model;
        }

        public static MethodModel Delete(this MethodModel model, string template = null)
        {
            model.HttpMethod = HttpMethods.Delete;
            model.Template = template;
            return model;
        }

        public static ParameterModel Parameter(this MethodModel model, string parameter)
        {
            return model.Parameters.First(p => p.Name == parameter);
        }

        public static ParameterModel FromBody(this ParameterModel model)
        {
            model.FromBody = true;
            return model;
        }

        public static ParameterModel FromQuery(this ParameterModel model, string name = null)
        {
            model.FromQuery = name ?? model.Name;
            return model;
        }

        public static ParameterModel FromRoute(this ParameterModel model, string name = null)
        {
            model.FromRoute = name ?? model.Name;
            return model;
        }

        public static ParameterModel FromHeader(this ParameterModel model, string name = null)
        {
            model.FromHeader = name ?? model.Name;
            return model;
        }
    }
}
