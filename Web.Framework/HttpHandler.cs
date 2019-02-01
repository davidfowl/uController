using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Web.Framework
{
    public abstract partial class HttpHandler
    {
        public Result Execute(RequestDelegate requestDelegate) => new InlineResult(requestDelegate);
        public Result Empty() => Execute(_ => Task.CompletedTask);
        public Result BadRequest() => Status(StatusCodes.Status400BadRequest);
        public Result NotFound() => Status(StatusCodes.Status404NotFound);
        public Result Ok() => Status(StatusCodes.Status200OK);
        public Result Ok(object value) => new ObjectResult(value);
        public Result Status(int statusCode) => new StatusCodeResult(statusCode);
    }
}
