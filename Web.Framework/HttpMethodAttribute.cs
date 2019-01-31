using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;

namespace Web.Framework
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class HttpMethodAttribute : Attribute, IHttpMethodMetadata
    {
        public string Method { get; }
        public string Template { get; set; }

        public bool AcceptCorsPreflight => false;

        public IReadOnlyList<string> HttpMethods { get; }

        public HttpMethodAttribute(string method, string template)
        {
            Method = method;
            Template = template;
            HttpMethods = new List<string>() { method };
        }
    }

    public class HttpGetAttribute : HttpMethodAttribute
    {
        public HttpGetAttribute(string template = null) : base(Microsoft.AspNetCore.Http.HttpMethods.Get, template)
        {

        }
    }

    public class HttpPostAttribute : HttpMethodAttribute
    {
        public HttpPostAttribute(string template = null) : base(Microsoft.AspNetCore.Http.HttpMethods.Post, template)
        {

        }
    }

    public class HttpDeleteAttribute : HttpMethodAttribute
    {
        public HttpDeleteAttribute(string template = null) : base(Microsoft.AspNetCore.Http.HttpMethods.Delete, template)
        {

        }
    }

    public class HttpPutAttribute : HttpMethodAttribute
    {
        public HttpPutAttribute(string template = null) : base(Microsoft.AspNetCore.Http.HttpMethods.Put, template)
        {

        }
    }
}
