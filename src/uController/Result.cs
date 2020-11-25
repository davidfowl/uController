using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace uController
{
    // Result is the interface version of a RequestDelegate
    public interface IResult
    {
        Task ExecuteAsync(HttpContext httpContext);
    }
}
