using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using uController;

namespace Samples
{
    public class NewtonsoftJsonResponseWriter : IHttpResponseWriter
    {
        public async Task WriteAsync(HttpContext httpContext, object value)
        {
            httpContext.Response.ContentType = "application/json";

            await using var bufferingStream = new FileBufferingWriteStream();

            using (var writer = new HttpResponseStreamWriter(bufferingStream, Encoding.UTF8))
            {
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    var json = new JsonSerializer();

                    json.Serialize(jsonWriter, value);
                }
            }

            await bufferingStream.DrainBufferAsync(httpContext.Response.Body);
        }
    }
}
