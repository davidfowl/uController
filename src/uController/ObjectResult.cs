using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace uController
{
    public class ObjectResult : IResult
    {
        private static readonly JsonResponseWriter _writer = new JsonResponseWriter();

        public object Value { get; }

        public ObjectResult(object value)
        {
            Value = value;
        }

        public Task ExecuteAsync(HttpContext httpContext)
        {
            var responseFormatter = httpContext.RequestServices.GetService<IHttpResponseWriter>() ?? _writer;

            return responseFormatter.WriteAsync(httpContext, Value);
        }
    }
}
