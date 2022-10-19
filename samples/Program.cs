using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder();

var app = builder.Build();

app.MapGet("/", () => "Hello World");
app.MapGet("/hello", () => "Hello World");

app.Run();