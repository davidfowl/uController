using System.Collections.Generic;
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
        public void Execute(SourceGeneratorContext context)
        {
            // For debugging
            //while (!System.Diagnostics.Debugger.IsAttached)
            //{
            //    System.Threading.Thread.Sleep(1000);
            //}

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
            }
        }

        public void Initialize(InitializationContext context)
        {
            // No initialization required
        }
    }
}
