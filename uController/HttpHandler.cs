using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace uController
{
    public abstract partial class HttpHandler
    {
        public IResult Empty() => new EmptyResult();
        public IResult BadRequest() => Status(StatusCodes.Status400BadRequest);
        public IResult NotFound() => Status(StatusCodes.Status404NotFound);
        public IResult Ok() => Status(StatusCodes.Status200OK);
        public IResult Ok(object value) => new ObjectResult(value);
        public IResult Status(int statusCode) => new StatusCodeResult(statusCode);
    }
}
