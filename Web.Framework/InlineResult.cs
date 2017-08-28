using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Web.Framework
{
    public class InlineResult : Result
    {
        private RequestDelegate _callback;

        public InlineResult(RequestDelegate callback)
        {
            _callback = callback;
        }

        public override Task ExecuteAsync(HttpContext httpContext) => _callback(httpContext);
    }
}