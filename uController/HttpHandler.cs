using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace uController
{
    public abstract partial class HttpHandler
    {
        public static IResult BadRequest() => Status(StatusCodes.Status400BadRequest);
        public static IResult NotFound() => Status(StatusCodes.Status404NotFound);
        public static IResult Ok() => Status(StatusCodes.Status200OK);
        public static IResult Ok(object value) => new ObjectResult(value);
        public static IResult Status(int statusCode) => new StatusCodeResult(statusCode);
    }
}
