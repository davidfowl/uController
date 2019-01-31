using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;

namespace Web.Framework
{
    public interface IHttpRequestFormatter
    {
        // Temporary because we can't codegen async code
        T Read<T>(HttpContext httpContext);
    }

    public class JsonRequestFormatter : IHttpRequestFormatter
    {
        public T Read<T>(HttpContext httpContext)
        {
            // Hard coded to JSON (and JSON.NET at that!)
            // Also this is synchronous but we buffer the request body

            var obj = new JsonSerializer().Deserialize<T>(
                new JsonTextReader(
                    new HttpRequestStreamReader(
                       httpContext.Request.Body, Encoding.UTF8)));

            return obj;
        }
    }
}
