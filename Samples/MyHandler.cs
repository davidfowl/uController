using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Web.Framework;

namespace Samples
{
    public class MyHandler : HttpHandler
    {
        [HttpGet]
        public async Task<Result> Get([FromQuery]int? id)
        {
            await Task.Delay(100);

            return Json(new { A = id.GetValueOrDefault() });
        }

        [HttpGet("/foo")]
        public Task Another(HttpContext context)
        {
            return context.Response.WriteAsync("Hello World");
        }

        [HttpGet("/hello")]
        public string Get() => "Hey!";

        [HttpPost]
        public Result Post([FromBody]JToken obj)
        {
            return Json(obj);
        }
    }
}