using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Web.Framework
{
    public interface IHttpResponseWriter
    {
        Task WriteAsync(HttpContext httpContext, object value);
    }
}
