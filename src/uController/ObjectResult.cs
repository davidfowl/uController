using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace uController
{
    public class ObjectResult : IResult
    {
        public object Value { get; }

        public ObjectResult(object value)
        {
            Value = value;
        }

        public Task ExecuteAsync(HttpContext httpContext)
        {
            return httpContext.Response.WriteAsJsonAsync(Value);
        }
    }
}
