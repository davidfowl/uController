using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using uController;

namespace Samples
{
    public class NewtonsoftJsonRequestReader : IHttpRequestReader
    {
        public async ValueTask<object> ReadAsync(HttpContext httpContext, Type targetType)
        {
            var request = httpContext.Request;
            if (!request.Body.CanSeek)
            {
                // JSON.Net does synchronous reads. In order to avoid blocking on the stream, we asynchronously
                // read everything into a buffer, and then seek back to the beginning.
                request.EnableBuffering();
                Debug.Assert(request.Body.CanSeek);

                await request.Body.DrainAsync(CancellationToken.None);
                request.Body.Seek(0L, SeekOrigin.Begin);
            }
            
            var obj = new JsonSerializer().Deserialize(
                new JsonTextReader(
                    new HttpRequestStreamReader(
                       httpContext.Request.Body, Encoding.UTF8)), targetType);

            return obj;
        }
    }
}
