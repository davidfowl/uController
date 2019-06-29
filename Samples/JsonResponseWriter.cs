using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Web.Framework;

namespace Samples
{
    internal class JsonResponseWriter : IHttpResponseWriter
    {
        public Task WriteAsync(HttpContext httpContext, object value)
        {
            return JsonSerializer.SerializeAsync(httpContext.Response.Body, value, value?.GetType());
        }
    }
}