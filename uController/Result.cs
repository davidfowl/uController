using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace uController
{
    // Result is the class version of a RequestDelegate
    // it can be used as a return value so it can be observed, but has an implcit
    // conversion to a RequestDelegate so it can be used any where
    // a RequestDelegate is accepted e.g:
    // app.Run(new ObjectResult(new { A = 1 }));
    public interface IResult
    {
        Task ExecuteAsync(HttpContext httpContext);
    }
}
