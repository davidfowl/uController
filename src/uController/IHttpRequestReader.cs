using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace uController
{
    public interface IHttpRequestReader
    {
        ValueTask<object> ReadAsync(HttpContext httpContext, Type targetType);
    }
}
