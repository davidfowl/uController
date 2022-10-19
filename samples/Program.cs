using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder();

var app = builder.Build();

app.MapGet("/", () => "Hello World");
app.MapGet("/hello/{name}", (string name) => $"Hello {name}");

app.MapGet("/person", () => new Person("David"));

app.MapGet("/ok", () => Results.NotFound());

app.Run();

record Person(string Name);