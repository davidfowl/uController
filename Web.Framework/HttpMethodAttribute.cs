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

        public HttpMethodAttribute(string method)
        {
            Method = method;
        }
    }

    public class HttpGetAttribute : HttpMethodAttribute
    {
        public HttpGetAttribute() : base(HttpMethods.Get)
        {

        }
    }

    public class HttpPostAttribute : HttpMethodAttribute
    {
        public HttpPostAttribute() : base(HttpMethods.Post)
        {

        }
    }

    public class HttpDeleteAttribute : HttpMethodAttribute
    {
        public HttpDeleteAttribute() : base(HttpMethods.Delete)
        {

        }
    }

    public class HttpPutAttribute : HttpMethodAttribute
    {
        public HttpPutAttribute() : base(HttpMethods.Put)
        {

        }
    }
}
