using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using uController.CodeGeneration;

namespace uController.SourceGenerator
{
    [Generator]
    public class uControllerGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
            {
                // nothing to do yet
                return;
            }

            // For debugging
            // System.Diagnostics.Debugger.Launch();

            var metadataLoadContext = new MetadataLoadContext(context.Compilation);
            var assembly = metadataLoadContext.MainAssembly;
            var uControllerAssembly = metadataLoadContext.LoadFromAssemblyName("uController");

            var models = new List<HttpModel>();

            foreach (var handlerType in receiver.HandlerTypes)
            {
                var semanticModel = context.Compilation.GetSemanticModel(handlerType.SyntaxTree);
                var symbol = semanticModel.GetDeclaredSymbol(handlerType);

                var type = assembly.GetType(symbol.ToDisplayString());
                var model = HttpModel.FromType(type, uControllerAssembly);
                models.Add(model);
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
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<TypeDeclarationSyntax> HandlerTypes { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is TypeDeclarationSyntax { AttributeLists: { Count: > 0 } } typeDecl &&
                    typeDecl.AttributeLists.SelectMany(s => s.Attributes).Any(s => s.Name.ToFullString() == "HttpHandler"))
                {
                    HandlerTypes.Add(typeDecl);
                }
            }
        }
    }
}
