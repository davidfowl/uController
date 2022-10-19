﻿using System.Security.Claims;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Patterns;

var builder = WebApplication.CreateBuilder();

var app = builder.Build();

app.MapGet("/", () => "Hello World");
app.MapGet("/hello/{name}", ([FromRoute]string name) => $"Hello {name}");

app.MapGet("/person", () => new Person("David"));

app.MapGet("/ok", (ClaimsPrincipal c) => Results.NotFound());

app.MapPost("/", ([FromBody] JsonNode node) => node).AddEndpointFilter((context, next) =>
{
    return next(context);
});

IResult NoAccess(int? id) => Results.StatusCode(401); 

app.Map("/private", NoAccess);

var s = "/something";

// This doesn't work yet
// app.MapGet(s, new Wrapper().Hello);

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