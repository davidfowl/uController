using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using MinimalApis.Extensions.Binding;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Net.Http;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Security.Claims;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace uController.SourceGenerator.Tests;

public class TestProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddSingleton<ISayHello, EnglishHello>();

        var app = builder.Build();

        app.MapGet("/", () => "Hello World");
        app.MapGet("/hello/{name}", (string name) => $"Hello {name}");

        app.MapGet("/anon", () => new { Name = "David" });

        app.MapGet("/person", () => new Person("David"));

        app.MapGet("/ok", (ClaimsPrincipal c, ISayHello hellosvc) => Results.Ok(hellosvc.Hello()));

        app.MapPost("/implictbody", (JsonNode node) => node).AddEndpointFilter((context, next) => next(context));
        app.MapPost("/", ([FromBody] JsonNode node) => node).AddEndpointFilter((context, next) => next(context));

        app.MapPost("/model", (Model m) => m);
        app.MapPost("/model2", (Model m) => { });

        app.MapPost("/fileupload", (IFormFile file) =>
        {
            return $"Uploaded {file.Name}";
        });

        app.MapGet("/nameofnot", (string name) => name is null ? Results.NotFound() : Results.Ok($"Hello {name}"));


        app.MapPost("/formpost", (IFormCollection formCollection) =>
        {
            return $"Uploaded {formCollection.Count} files";
        });

        app.MapPost("/body", (Body<string> body) => body.Value);

        app.MapPost("/validated", (Validated<Model> model) =>
        {
            if (!model.IsValid)
            {
                return Results.ValidationProblem(model.Errors);
            }
            return Results.Ok();
        });

        app.MapGet("/something", object (CancellationToken ct) => new Person("Hello"));
        IResult NoAccess(int? id) => Results.StatusCode(401);

        app.Map("/private", NoAccess);

        app.MapPost("/post/q", (Stream s) => Results.Stream(s));
        app.MapPost("/post/q", (PipeReader r) => Results.Stream(r.AsStream()));

        app.MapPatch("/patch", (HttpRequest req, HttpResponse resp) => Task.CompletedTask);

        app.MapGet("/multiple", (StringValues queries) => queries.ToArray());
        app.MapGet("/multiple2", (string[] queries) => queries);
        app.MapGet("/multiple3", (int[] queries) => queries);

        var api = app.MapGroup("/api");

        var personGroup = api.MapGroup("/persons");
        personGroup.MapGet("/", () => new Person("David"));

        // api.MapTodoEndpoints();

        var s = Wrapper.RoutePattern;

        app.MapGet(s, new Wrapper().Hello);

        app.MapGet("/another", Wrapper.HelloDelegate);
        app.MapGet("/another1", Wrapper.HelloDelegate2);

        app.MapGet("/choose/{c}", (Choices c) => c.ToString());

        var wrapper = new Wrapper();
        wrapper.AddRoutes(app);

        var d = wrapper.Hello;

        // This can't be resolved
        // app.MapGet("/del", d);

        // var path = "/foo/{s}";

        var f = (string s) => "hello";

        // This neither
        // app.MapGet(path, f);

        // void Helper(string s, Func<string, string> handler) => app.MapGet(s, handler);

        app.Map("/test/map", (int x) => x);
        app.Map("/test/map", (int x, int y) => { });
        app.MapPut("/test/put", (int x) => x);
        app.MapPut("/test/put", (int x, int y) => { });
        app.MapPost("/test/post", (int x) => x);
        app.MapPost("/test/post", (int x, int y) => { });
        app.MapDelete("/test/delete", (int x) => x);
        app.MapDelete("/test/delete", (int x, int y) => { });
        app.MapPatch("/test/patch", (int x) => x);
        app.MapPatch("/test/patch", (int x, int y) => { });
        app.Map("/test/{n}", (int n) => n);

        //void Foo([FromBody] JsonNode a1, [FromBody] JsonNode a2)
        //{

        //}

        //app.MapGet("/twobodies", Foo);

        app.MapGet("/something/parsable", ([FromQuery] Parsable p) => p);

        //app.MapGet("/lineinfo", string (HttpContext context) =>
        //{
        //    var sourceKey = context.GetEndpoint()?.Metadata.GetMetadata<SourceKey>();
        //    return sourceKey.ToString();
        //});

        app.Run();
    }
}

record Person(string Name);
record Product(string Name, decimal Price);

struct Parsable : IParsable<Parsable>
{
    public Parsable()
    {

    }

    public static Parsable Parse(string s, IFormatProvider provider)
    {
        throw new NotImplementedException();
    }

    public static bool TryParse([NotNullWhen(true)] string s, IFormatProvider provider, [MaybeNullWhen(false)] out Parsable result)
    {
        throw new NotImplementedException();
    }
}

class Wrapper
{
    public static readonly string RoutePattern = "/foo/{id}";

    public static readonly Func<string, string> HelloDelegate = Hello2;

    public static readonly Func<string, string> HelloDelegate2 = (name) => $"Hello {name}";

    public static string Hello2(string name) => $"Hello {name}";

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
    public static ValueTask<Model> BindAsync(HttpContext context, ParameterInfo pi)
    {
        return ValueTask.FromResult(new Model());
    }
}

enum Choices
{
    One,
    Two,
    Three,
}