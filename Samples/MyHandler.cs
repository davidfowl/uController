using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Web.Framework;

namespace WebApplication45
{
    public class MyHandler : HttpHandler
    {
        [HttpGet]
        public async Task<Result> Get([FromQuery]int? id)
        {
            await Task.Delay(100);

            return Json(new { A = id.GetValueOrDefault() });
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