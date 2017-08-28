using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Web.Framework
{
    public class EmptyResult : Result
    {
        public override Task ExecuteAsync(HttpContext httpContext) => Task.CompletedTask;
    }
}