using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace uController
{
    public class StatusCodeResult : Result
    {
        public int StatusCode { get; }

        public StatusCodeResult(int statusCode)
        {
            StatusCode = statusCode;
        }

        public override Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = StatusCode;
            return Task.CompletedTask;
        }
    }
}
