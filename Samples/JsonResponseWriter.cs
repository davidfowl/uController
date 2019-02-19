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
            var task = httpContext.Response.StartAsync();

            if (!task.IsCompletedSuccessfully)
            {
                return AwaitWriteAsync(task, httpContext, value);
            }

            static async Task AwaitWriteAsync(Task startTask, HttpContext httpContext, object value)
            {
                await startTask;

                await JsonSerializer.WriteAsync(value, value.GetType(), httpContext.Response.BodyPipe);
            }

            return JsonSerializer.WriteAsync(value, value.GetType(), httpContext.Response.BodyPipe);
        }
    }
}