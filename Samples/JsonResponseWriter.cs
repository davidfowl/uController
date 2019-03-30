using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Web.Framework;

namespace Samples
{
    internal class JsonResponseWriter : IHttpResponseWriter
    {
        public Task WriteAsync(HttpContext httpContext, object value)
        {
            return JsonSerializer.WriteAsync(value, value.GetType(), httpContext.Response.Body);
        }
    }
}