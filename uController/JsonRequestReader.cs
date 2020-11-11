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
            return httpContext.Request.ReadFromJsonAsync(targetType, _serializerOptions);
        }
    }
}