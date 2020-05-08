using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace uController
{
    public class JsonRequestReader : IHttpRequestReader
    {
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public ValueTask<object> ReadAsync(HttpContext httpContext, Type targetType)
        {
            return JsonSerializer.DeserializeAsync(httpContext.Request.Body, targetType, _serializerOptions);
        }
    }
}