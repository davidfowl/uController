using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace uController.SourceGenerator.Tests;

public class IntegrationTests
{
    private readonly ITestOutputHelper _output;

    public IntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task MapGet_NoParameters_StringReturn()
    {
        // Arrange
        var source = @"
app.MapGet(""/"", () => ""Hello world!"");
";

        // Act
        var (results, compilation) = await RunGenerator(source);

        // Assert
        Assert.Empty(results.Diagnostics);

        var builderFunc = CreateInvocationFromCompilation(compilation);
        var builder = CreateEndpointBuilder();
        _ = builderFunc(builder);

        var dataSource = Assert.Single(builder.DataSources);
        var endpoint = Assert.Single(dataSource.Endpoints);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("GET", method);

        await AssertEndpointBehavior(endpoint, "Hello world!", 200);
    }

    [Fact]
    public async Task MapGet_StringRouteParameters_StringReturn()
    {
        // Arrange
        var source = @"
app.MapGet(""/hello/{name}"", (string name) => $""Hello {name}!"");
";

        // Act
        var (results, compilation) = await RunGenerator(source);

        // Assert
        Assert.Empty(results.Diagnostics);

        var builderFunc = CreateInvocationFromCompilation(compilation);
        var builder = CreateEndpointBuilder();
        _ = builderFunc(builder);

        var dataSource = Assert.Single(builder.DataSources);
        var endpoint = Assert.Single(dataSource.Endpoints);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("GET", method);

        await AssertEndpointBehavior(
            endpoint,
            "Hello Tester!",
            200,
            routeValues: new(new[] { new KeyValuePair<string, string?>("name", "Tester") }));
    }

    private static async Task AssertEndpointBehavior(
        Endpoint endpoint,
        string expectedResponse,
        int expectedStatusCode,
        RouteValueDictionary? routeValues = null)
    {
        var httpContext = new DefaultHttpContext();
        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;
        if (routeValues is not null)
        {
            httpContext.Request.RouteValues = routeValues;
        }

        await endpoint.RequestDelegate!(httpContext);

        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = streamReader.ReadToEndAsync().Result;
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        Assert.Equal(expectedResponse, body);
    }

    private static Func<IEndpointRouteBuilder, IEndpointRouteBuilder>  CreateInvocationFromCompilation(Compilation compilation)
    {
        using var output = new MemoryStream();
        var result = compilation.Emit(output);
        if (!result.Success)
        {
            throw new OperationCanceledException("Errors during compilation. Inspect diagnostics for more info.");
        }
        output.Seek(0, SeekOrigin.Begin);
        var assembly = AssemblyLoadContext.Default.LoadFromStream(output);
        return assembly?.GetType("TestMapActions")
                       ?.GetMethod("MapTestEndpoints", BindingFlags.Public | BindingFlags.Static)
                       ?.CreateDelegate<Func<IEndpointRouteBuilder, IEndpointRouteBuilder>>() ?? throw new InvalidOperationException("Unable to resolve map routes delegate");
    }

    private static async Task<(GeneratorRunResult, Compilation)> RunGenerator(string mapAction)
    {
        var project = CreateProject();
        var source = $@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

public static class TestMapActions
{{
    public static IEndpointRouteBuilder MapTestEndpoints(this IEndpointRouteBuilder app)
    {{
        {mapAction}
        return app;
    }}
}}";
        project = project.AddDocument("TestMapActions.cs", source).Project;
        var driver = (GeneratorDriver)CSharpGeneratorDriver.Create(new uControllerGenerator());
        var compilation = await project.GetCompilationAsync();

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var _);
        var results = driver.GetRunResult();
        var diagnostics = outputCompilation.GetDiagnostics();
        Assert.Empty(diagnostics);
        return (results.Results[0], outputCompilation);
    }

    private static Project CreateProject()
    {
        var projectId = ProjectId.CreateNewId(debugName: "TestProject");

        var solution = new AdhocWorkspace()
           .CurrentSolution
           .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp);

        var project = solution.Projects.Single()
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithNullableContextOptions(NullableContextOptions.Enable))
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp11));

        foreach (var defaultCompileLibrary in DependencyContext.Load(typeof(IntegrationTests).Assembly).CompileLibraries)
        {
            foreach (var resolveReferencePath in defaultCompileLibrary.ResolveReferencePaths(new AppLocalResolver()))
            {
                if (resolveReferencePath.EndsWith("SourceGenerator.dll"))
                {
                    continue;
                }
                project = project.AddMetadataReference(MetadataReference.CreateFromFile(resolveReferencePath));
            }
        }

        return project;
    }

    
    private static IEndpointRouteBuilder CreateEndpointBuilder()
    {
        return new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
    }

    private class AppLocalResolver : ICompilationAssemblyResolver
    {
        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
        {
            foreach (var assembly in library.Assemblies)
            {
                var dll = Path.Combine(Directory.GetCurrentDirectory(), "refs", Path.GetFileName(assembly));
                if (File.Exists(dll))
                {
                    assemblies.Add(dll);
                    return true;
                }

                dll = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(assembly));
                if (File.Exists(dll))
                {
                    assemblies.Add(dll);
                    return true;
                }
            }

            return false;
        }
    }

    private class EmptyServiceProvider : IServiceScope, IServiceProvider, IServiceScopeFactory
    {
        public IServiceProvider ServiceProvider => this;

        public RouteHandlerOptions RouteHandlerOptions { get; set; } = new RouteHandlerOptions();

        public IServiceScope CreateScope()
        {
            return this;
        }

        public void Dispose() { }

        public object? GetService(Type serviceType)
        {
            return null;
        }
    }

    private class DefaultEndpointRouteBuilder : IEndpointRouteBuilder
    {
        public DefaultEndpointRouteBuilder(IApplicationBuilder applicationBuilder)
        {
            ApplicationBuilder = applicationBuilder ?? throw new ArgumentNullException(nameof(applicationBuilder));
            DataSources = new List<EndpointDataSource>();
        }

        public IApplicationBuilder ApplicationBuilder { get; }

        public IApplicationBuilder CreateApplicationBuilder() => ApplicationBuilder.New();

        public ICollection<EndpointDataSource> DataSources { get; }

        public IServiceProvider ServiceProvider => ApplicationBuilder.ApplicationServices;
    }
}