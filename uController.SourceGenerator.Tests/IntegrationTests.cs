namespace uController.SourceGenerator.Tests;

public class IntegrationTests
{
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

        var sourceKeyMetadata = endpoint.Metadata.GetMetadata<SourceKey>();
        Assert.NotNull(sourceKeyMetadata);

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

        var sourceKeyMetadata = endpoint.Metadata.GetMetadata<SourceKey>();
        Assert.NotNull(sourceKeyMetadata);

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

    [Fact]
    public async Task MapGet_StringQueryParameters_StringReturn()
    {
        // Arrange
        var source = @"
app.MapGet(""/hello"", (string name) => $""Hello {name}!"");
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

        var sourceKeyMetadata = endpoint.Metadata.GetMetadata<SourceKey>();
        Assert.NotNull(sourceKeyMetadata);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("GET", method);

        await AssertEndpointBehavior(
            endpoint,
            "Hello David!",
            200,
            query: QueryString.Create("name", "David"));
    }

    private static async Task AssertEndpointBehavior(
        Endpoint endpoint,
        string expectedResponse,
        int expectedStatusCode,
        RouteValueDictionary? routeValues = null,
        QueryString? query = null)
    {
        var httpContext = new DefaultHttpContext();

        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;

        if (query is { } q)
        {
            httpContext.Request.QueryString = q;
        }

        if (routeValues is not null)
        {
            httpContext.Request.RouteValues = routeValues;
        }

        await endpoint.RequestDelegate!(httpContext);

        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        Assert.Equal(expectedResponse, body);
    }

    private static Func<IEndpointRouteBuilder, IEndpointRouteBuilder> CreateInvocationFromCompilation(Compilation compilation)
    {
        var output = new MemoryStream();
        var pdb = new MemoryStream();
        var result = compilation.Emit(output, pdb);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity > DiagnosticSeverity.Info));
        Assert.True(result.Success);

        output.Position = 0;
        pdb.Position = 0;

        var assembly = AssemblyLoadContext.Default.LoadFromStream(output, pdb);
        var handler = assembly?.GetType("TestMapActions")
                       ?.GetMethod("MapTestEndpoints", BindingFlags.Public | BindingFlags.Static)
                       ?.CreateDelegate<Func<IEndpointRouteBuilder, IEndpointRouteBuilder>>();

        Assert.NotNull(handler);
        return handler;
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
        project = project.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8)).Project;
        var compilation = await project.GetCompilationAsync();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new uControllerGenerator() },
            parseOptions: (CSharpParseOptions)project.ParseOptions!);

        Assert.NotNull(compilation);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var _);

        var results = driver.GetRunResult();
        var diagnostics = outputCompilation.GetDiagnostics();
        Assert.Empty(diagnostics.Where(d => d.Severity > DiagnosticSeverity.Info));
        return (results.Results[0], outputCompilation);
    }

    private static Project CreateProject()
    {
        var projectName = $"TestProject-{Guid.NewGuid()}";
        var projectId = ProjectId.CreateNewId(projectName);

        var solution = new AdhocWorkspace()
           .CurrentSolution
           .AddProject(projectId, projectName, projectName, LanguageNames.CSharp);

        var project = solution.Projects.Single()
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithNullableContextOptions(NullableContextOptions.Enable))
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp11).WithPreprocessorSymbols("NET7_0_OR_GREATER"));

        foreach (var defaultCompileLibrary in DependencyContext.Load(typeof(IntegrationTests).Assembly)!.CompileLibraries)
        {
            foreach (var resolveReferencePath in defaultCompileLibrary.ResolveReferencePaths(new AppLocalResolver()))
            {
                if (resolveReferencePath.Equals(typeof(uControllerGenerator).Assembly.Location))
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
        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string>? assemblies)
        {
            foreach (var assembly in library.Assemblies)
            {
                var dll = Path.Combine(Directory.GetCurrentDirectory(), "refs", Path.GetFileName(assembly));
                if (File.Exists(dll))
                {
                    assemblies ??= new();
                    assemblies.Add(dll);
                    return true;
                }

                dll = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(assembly));
                if (File.Exists(dll))
                {
                    assemblies ??= new();
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