using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using uController;
using static uController.Results;

namespace Samples
{
    public class MyHandler
    {
        [HttpGet("/")]
        public Task Get(HttpContext context)
        {
            return context.Response.WriteAsync("Hello World");
        }

        [HttpGet("/blah")]
        public object Blah()
        {
            return new { name = "David Fowler" };
        }

        [HttpGet("/status/{status}")]
        public IResult StatusCode([FromRoute] int status)
        {
            return Status(status);
        }

        [HttpGet("/slow/status/{status}")]
        public async Task<IResult> SlowTaskStatusCode()
        {
            await Task.Delay(1000);

            return StatusCode(400);
        }

        [HttpGet("/fast/status/{status}")]
        public ValueTask<IResult> FastValueTaskStatusCode([FromServices] ILoggerFactory loggerFactory)
        {
            return new ValueTask<IResult>(Status(201));
        }

        [HttpGet("/lag")]
        public async Task DoAsync(HttpContext context, [FromQuery] string q)
        {
            await Task.Delay(100);

            await context.Response.WriteAsync(q);
        }

        [HttpGet("/hey/david")]
        public string HelloDavid() => "Hello David!";

        [HttpGet("/hey/{name?}")]
        public async Task<string> GetAsync([FromRoute] string name)
        {
            await Task.Delay(500);

            return $"Hey {name ?? "David"}!";
        }

        [HttpGet("/hello")]
        public string Hello([FromForm("foo")] string s, [FromHeader("X-Id")] string id, [FromQuery] int? page, [FromQuery] int? pageSize) => "Hello!";

        [HttpPost("/")]
        public IResult Post([FromBody] Person obj)
        {
            return Ok(obj);
        }

        [HttpPost("/post-form")]
        public void PostAForm(IFormCollection form)
        {

        }

        [HttpGet("/auth")]
        [Authorize]
        public void Authed()
        {

        }
    }

    public class Person
    {
        public int Age { get; set; }
        public string Name { get; set; }
    }
}