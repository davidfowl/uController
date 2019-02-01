using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Web.Framework
{
    public interface IHttpRequestReader
    {
        ValueTask<object> ReadAsync(HttpContext httpContext, Type targetType);
    }
}
