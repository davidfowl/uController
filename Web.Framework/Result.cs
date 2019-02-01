using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Web.Framework
{
    // Result is the class version of a RequestDelegate
    // it can be used as a return value so it can be observed, but has an implcit
    // conversion to a RequestDelegate so it can be used any where
    // a RequestDelegate is accepted e.g:
    // app.Run(new ObjectResult(new { A = 1 }));
    public abstract class Result
    {
        public abstract Task ExecuteAsync(HttpContext httpContext);

        public static implicit operator RequestDelegate(Result result)
        {
            return result.ExecuteAsync;
        }
    }
}
