using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Web.Framework;

namespace Samples
{
    internal class JsonRequestReader : IHttpRequestReader
    {
        public async ValueTask<object> ReadAsync(HttpContext httpContext, Type targetType)
        {
            return await JsonSerializer.ReadAsync(httpContext.Request.BodyPipe, targetType);
        }
    }
}