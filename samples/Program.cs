using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder();

var app = builder.Build();

app.MapGet("/", () => "Hello World");
app.MapGet("/hello/{name}", ([FromRoute]string name) => $"Hello {name}");

app.MapGet("/person", () => new Person("David"));

app.Run();

record Person(string Name);