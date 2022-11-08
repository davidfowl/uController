using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace uController.SourceGenerator.Tests;

internal class ApiApplication : WebApplicationFactory<TestProgram>
{
    private readonly string _environment;

    public ApiApplication(string environment = "Development")
    {
        _environment = environment;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        return base.CreateHost(builder);
    }
}