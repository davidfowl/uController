using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Sample;
using System.Security.Claims;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder();

builder.Services.AddSingleton<ISayHello, EnglishHello>();

var app = builder.Build();

app.MapGet("/", () => "Hello World");
app.MapGet("/hello/{name}", (string name) => $"Hello {name}");

app.MapGet("/person", () => new Person("David"));

app.MapGet("/ok", (ClaimsPrincipal c, [FromServices] ISayHello hellosvc) => Results.Ok(hellosvc.Hello()));

app.MapPost("/", (JsonNode node) => node).AddEndpointFilter((context, next) => next(context));

app.MapPost("/model", (Model m) => m);
app.MapPost("/model2", (Model m) => { });

app.MapGet("/something", object (CancellationToken ct) => new Person("Hello"));

IResult NoAccess(int? id) => Results.StatusCode(401);

app.Map("/private", NoAccess);

app.MapPatch("/patch", (HttpRequest req, HttpResponse resp) => Task.CompletedTask);

app.MapGet("/multiple", (StringValues queries) => queries.ToArray());

var api = app.MapGroup("/api");

var personGroup = api.MapGroup("/persons");
personGroup.MapGet("/", () => new Person("David"));

api.MapProducts();

var s = Wrapper.RoutePattern;

app.MapGet(s, new Wrapper().Hello);

var wrapper = new Wrapper();
wrapper.AddRoutes(app);

var d = wrapper.Hello;

app.MapGet("/del", d);

var path = "/foo/{s}";

var f = (string s) => "hello";

app.MapGet(path, f);

app.Run();

record Person(string Name);
record Product(string Name, decimal Price);

class Wrapper
{
    public static string RoutePattern = "/foo/{s}";

    public string Hello(int id) => "Hello World";

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