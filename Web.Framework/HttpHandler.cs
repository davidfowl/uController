using Microsoft.AspNetCore.Http;

namespace Web.Framework
{
    public abstract partial class HttpHandler
    {
        public HttpContext HttpContext { get; set; }
        public RequestDelegate NextMiddleware { get; set; }

        public Result Next() => Result(NextMiddleware);
        public Result Result(RequestDelegate requestDelegate) => new InlineResult(requestDelegate);
        public Result Empty() => new EmptyResult();
        public Result BadRequest() => Status(StatusCodes.Status400BadRequest);
        public Result NotFound() => Status(StatusCodes.Status404NotFound);
        public Result Ok() => Status(StatusCodes.Status200OK);
        public Result Status(int statusCode) => new StatusCodeResult(statusCode);
        public Result Json(object value) => new JsonResult(value);
    }
}
