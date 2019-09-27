using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace uController
{
    public interface IHttpResponseWriter
    {
        Task WriteAsync(HttpContext httpContext, object value);
    }
}
