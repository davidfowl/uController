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

            var endpointRouteBuilderType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Routing.IEndpointRouteBuilder");

            foreach (var (memberAccess, handlerType) in receiver.MapHandlerCalls)
            {
                var semanticModel = context.Compilation.GetSemanticModel(memberAccess.Expression.SyntaxTree);
                var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);

                if (!SymbolEqualityComparer.Default.Equals(typeInfo.Type, endpointRouteBuilderType))
                {
                    continue;
                }

                semanticModel = context.Compilation.GetSemanticModel(handlerType.SyntaxTree);
                typeInfo = semanticModel.GetTypeInfo(handlerType);

                var type = assembly.GetType(typeInfo.Type.ToDisplayString());
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
            // System.Diagnostics.Debugger.Launch();

            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<(MemberAccessExpressionSyntax, TypeSyntax)> MapHandlerCalls { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is MemberAccessExpressionSyntax
                    { Name: GenericNameSyntax { TypeArgumentList: { Arguments: { Count: 1 } arguments } } genericName } memberAccessExpressionSyntax &&
                    genericName.Identifier.ToFullString() == "MapHttpHandler")
                {
                    MapHandlerCalls.Add((memberAccessExpressionSyntax, arguments[0]));
                }
            }
        }
    }
}
