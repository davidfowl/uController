using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace uController
{
    public class EmptyResult : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext) => Task.CompletedTask;
    }
}
