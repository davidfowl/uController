## uController (pronounced micro (μ) controller)

[![feedz.io](https://img.shields.io/badge/endpoint.svg?url=https%3A%2F%2Ff.feedz.io%2Fdavidfowl%2Fucontroller%2Fshield%2FuController%2Flatest&label=uController)](https://f.feedz.io/davidfowl/ucontroller/packages/uController/latest/download)

A declarative micro-framework inspired by ASP.NET Core MVC but using C# source generators.

- Built on top of ASP.NET Core
- AOT friendly (using source generators)

Differences from MVC
- No discovery of "controllers"
- No base class required 
- Support for basic and *very* efficient model binding (FromQuery, FromHeader, FromForm, FromBody)
- No validation or any extensiblity besides `IResult`
- One type of filter (not wired up yet)

There are 2 packages:

- **uController** - The main package that contains the framework primitives and runtime code generation logic.
- **uController.SourceGenerator** - The source generator that overrides the runtime code generation with compile time logic.

### Hello World

```C#
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHttpHandler<HelloHandler>();
        });
    }
}

public class HelloHandler
{
    [HttpGet("/")]
    public string Hello() => "Hello uController";
}
```

## Using CI Builds

To use CI builds add the following nuget feed:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <packageSources>
        <clear />
        <add key="ucontroller" value="https://f.feedz.io/davidfowl/ucontroller/nuget/index.json" />
        <add key="NuGet.org" value="https://api.nuget.org/v3/index.json" />
    </packageSources>
</configuration>
```

See the list of [versions](https://f.feedz.io/davidfowl/ucontroller/nuget/v3/packages/ucontroller/index.json)
