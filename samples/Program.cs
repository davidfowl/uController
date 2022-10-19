using System.Security.Claims;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder();

builder.Services.AddSingleton<ISayHello, EnglishHello>();

var app = builder.Build();

app.MapGet("/", () => "Hello World");
app.MapGet("/hello/{name}", (string name) => $"Hello {name}");

app.MapGet("/person", () => new Person("David"));

app.MapGet("/ok", (ClaimsPrincipal c, [FromServices]ISayHello s) => Results.Ok(s.Hello()));

app.MapPost("/", ([FromBody] JsonNode node) => node).AddEndpointFilter((context, next) =>
{
    return next(context);
});

app.MapPost("/model", (Model m) => m);

app.MapGet("/someting", object (CancellationToken ct) => new Person("Hello"));

IResult NoAccess(int? id) => Results.StatusCode(401); 

app.Map("/private", NoAccess);

app.MapPatch("/patch", (HttpRequest req, HttpResponse resp) => Task.CompletedTask);

var s = "/something";

// This doesn't work yet
app.MapGet(s, new Wrapper().Hello);

var wrapper = new Wrapper();
wrapper.AddRoutes(app);

app.Run();

record Person(string Name);

class Wrapper
{
    public string Hello() => "Hello World";

    public void AddRoutes(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/hello2", Hello);
    }
}
interface ISayHello
{
    string Hello();
}

class EnglishHello : ISayHello
{
    public string Hello() => "Hello";
}

public class Model
{
    public static ValueTask<Model> BindAsync(HttpContext context)
    {
        return ValueTask.FromResult(new Model());
    }
}