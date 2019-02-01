using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Web.Framework
{
    public class ObjectResult : Result
    {
        public object Value { get; }

        public ObjectResult(object value)
        {
            Value = value;
        }

        public override Task ExecuteAsync(HttpContext httpContext)
        {
            var responseFormatter = httpContext.RequestServices.GetRequiredService<IHttpResponseWriter>();

            return responseFormatter.WriteAsync(httpContext, Value);
        }
    }
}
