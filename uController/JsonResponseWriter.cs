using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace uController
{
    public class JsonResponseWriter : IHttpResponseWriter
    {
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public Task WriteAsync(HttpContext httpContext, object value)
        {
            return httpContext.Response.WriteAsJsonAsync(value, _serializerOptions);
        }
    }
}