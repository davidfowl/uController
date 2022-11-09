using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using uController.SourceGenerator;
using Microsoft.Extensions.DependencyModel;
using System.IO;
using Microsoft.Extensions.DependencyModel.Resolution;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc.Testing;

namespace uController.SourceGenerator.Tests;

public class IntegrationTests
{
    private readonly ITestOutputHelper _output;

    public IntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestSingleMapGet_Alt()
    {
        // Arrange
        var source = @"
var app = WebApplication.Create();
app.MapGet(""/"", () => ""Hello world!"");
app.Run();
";

        // Act
        var (results, compilation) = await RunGenerator(source);

        // Assert
        Assert.Empty(results.Diagnostics);
    }

    private static async Task<(GeneratorRunResult, Compilation)> RunGenerator(string source)
    {
        var project = CreateProject();
        project = project.AddDocument("Program.cs", source).Project;
        var driver = (GeneratorDriver)CSharpGeneratorDriver.Create(new uControllerGenerator());
        var compilation = await project.GetCompilationAsync();

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var _);
        var results = driver.GetRunResult();
        return (results.Results[0], outputCompilation);
    }

    private static Project CreateProject()
    {
        var projectId = ProjectId.CreateNewId(debugName: "TestProject");

        var solution = new AdhocWorkspace()
           .CurrentSolution
           .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp);

        var project = solution.Projects.Single()
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication)
            .WithNullableContextOptions(NullableContextOptions.Enable))
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.Preview));

        project = project.WithParseOptions(((CSharpParseOptions)project.ParseOptions!).WithLanguageVersion(LanguageVersion.Preview));


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

        // The deps file in the project is incorrect and does not contain "compile" nodes for some references.
        // However these binaries are always present in the bin output. As a "temporary" workaround, we'll add
        // every dll file that's present in the test's build output as a metadatareference.
        foreach (var assembly in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
        {
            if (!project.MetadataReferences.Any(c => string.Equals(Path.GetFileNameWithoutExtension(c.Display), Path.GetFileNameWithoutExtension(assembly), StringComparison.OrdinalIgnoreCase)))
            {
                project = project.AddMetadataReference(MetadataReference.CreateFromFile(assembly));
            }
        }

        return project;
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
}