using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;

namespace Web.Framework
{
    public interface IHttpResponseWriter
    {
        Task WriteAsync(HttpContext httpContext, object value);
    }

    public class JsonResponseWriter : IHttpResponseWriter
    {
        public async Task WriteAsync(HttpContext httpContext, object value)
        {
            httpContext.Response.ContentType = "application/json";

            using (var writer = new HttpResponseStreamWriter(httpContext.Response.Body, Encoding.UTF8))
            {
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    var json = new JsonSerializer();

                    json.Serialize(jsonWriter, value);

                    await writer.FlushAsync();
                }
            }
        }
    }
}
