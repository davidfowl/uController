using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;

namespace Web.Framework
{
    public class JsonResult : Result
    {
        public object Value { get; }

        public JsonResult(object value)
        {
            Value = value;
        }

        public override async Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.ContentType = "application/json";

            using (var writer = new HttpResponseStreamWriter(httpContext.Response.Body, Encoding.UTF8))
            {
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    var json = new JsonSerializer();

                    json.Serialize(jsonWriter, Value);

                    await writer.FlushAsync();
                }
            }
        }
    }
}
