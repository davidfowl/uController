using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Web.Framework
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class HttpMethodAttribute : Attribute
    {
        public string Method { get; }
        public string Template { get; set; }

        public HttpMethodAttribute(string method, string template)
        {
            Method = method;
            Template = template;
        }
    }

    public class HttpGetAttribute : HttpMethodAttribute
    {
        public HttpGetAttribute(string template = null) : base(HttpMethods.Get, template)
        {

        }
    }

    public class HttpPostAttribute : HttpMethodAttribute
    {
        public HttpPostAttribute(string template = null) : base(HttpMethods.Post, template)
        {

        }
    }

    public class HttpDeleteAttribute : HttpMethodAttribute
    {
        public HttpDeleteAttribute(string template = null) : base(HttpMethods.Delete, template)
        {

        }
    }

    public class HttpPutAttribute : HttpMethodAttribute
    {
        public HttpPutAttribute(string template = null) : base(HttpMethods.Put, template)
        {

        }
    }
}
