using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace uController
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
