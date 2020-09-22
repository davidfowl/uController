using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using uController.CodeGeneration;

namespace uController.SourceGenerator
{
    [Generator]
    public class uControllerGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // For debugging
            // System.Diagnostics.Debugger.Launch();

            var metadataLoadContext = new MetadataLoadContext(context.Compilation);
            var uControllerAssembly = metadataLoadContext.LoadFromAssemblyName("uController");
            var handler = uControllerAssembly.GetType(typeof(HttpHandler).FullName);
            var assembly = metadataLoadContext.MainAssembly;

            var models = new List<HttpModel>();

            foreach (var type in assembly.GetExportedTypes())
            {
                if (handler.IsAssignableFrom(type))
                {
                    var model = HttpModel.FromType(type);
                    models.Add(model);
                }
            }

            foreach (var model in models)
            {
                var gen = new CodeGenerator(model, metadataLoadContext);
                var rawSource = gen.Generate();
                var sourceText = SourceText.From(rawSource, Encoding.UTF8);

                // For debugging
                //var comp = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(sourceText));
                //var diagnosrics = comp.GetDiagnostics();

                context.AddSource(model.HandlerType.Name + "RouteExtensions", sourceText);

                //if (gen.FromBodyTypes.Any())
                //{
                //    var jsonGenerator = new JsonCodeGenerator(metadataLoadContext, model.HandlerType.Namespace);
                //    var generatedConverters = jsonGenerator.Generate(gen.FromBodyTypes, out var helperSource);
                //}
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required
        }
    }
}
