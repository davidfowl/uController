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
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
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

            foreach (var (memberAccess, handlerType) in receiver.MapHandlers)
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

            int number = 0;
            var sb = new StringBuilder();
            var formattedTypes = new HashSet<string>();

            foreach (var (invocation, lambda, arguments, returns) in receiver.MapActions)
            {
                var semanticModel = context.Compilation.GetSemanticModel(invocation.SyntaxTree);

                var types = new string[arguments.Length + 1];
                for (int i = 0; i < arguments.Length; i++)
                {
                    types[i] = semanticModel.GetTypeInfo(arguments[i]).Type.ToDisplayString();
                }

                var si = semanticModel.GetSymbolInfo(lambda);

                if (si.Symbol is IMethodSymbol method)
                {
                    types[arguments.Length] = method.ReturnType.ToDisplayString();
                }
                else
                {
                    foreach (var returnSyntax in returns)
                    {
                        var returnType = semanticModel.GetTypeInfo(returnSyntax);
                        // Pick first non null type
                        types[arguments.Length] = returnType.Type.ToDisplayString();
                    }
                }

                var formattedTypeArgs = string.Join(",", types);

                if (!formattedTypes.Add(formattedTypeArgs))
                {
                    continue;
                }

                var text = @$"public static void MapAction(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, string pattern, System.Func<{formattedTypeArgs}> callback)
        {{
            System.Console.WriteLine(callback.Method.MetadataToken);
        }}
";
                sb.Append(text);
                number++;
            }

            var mapActionsText = $@"
namespace Microsoft.AspNetCore.Routing
{{
    public static class MapActionsExtensions
    {{
        {sb}
    }}
}}";
            context.AddSource($"MapActionsExtensions", SourceText.From(mapActionsText, Encoding.UTF8));

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
            public List<(MemberAccessExpressionSyntax, TypeSyntax)> MapHandlers { get; } = new();

            public List<(InvocationExpressionSyntax, LambdaExpressionSyntax, TypeSyntax[], ExpressionSyntax[])> MapActions { get; } = new();

            public List<LocalFunctionStatementSyntax> LocalFunctions { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is MemberAccessExpressionSyntax
                    { Name: GenericNameSyntax { TypeArgumentList: { Arguments: { Count: 1 } arguments }, Identifier: { ValueText: "MapHttpHandler" } } } mapHandlerCall)
                {
                    MapHandlers.Add((mapHandlerCall, arguments[0]));
                }

                if (syntaxNode is InvocationExpressionSyntax
                    {
                        Expression: MemberAccessExpressionSyntax
                        {
                            Name: IdentifierNameSyntax
                            {
                                Identifier: { ValueText: "MapAction" }
                            }
                        },
                        ArgumentList: { Arguments: { Count: 2 } args }
                    } mapActionCall && args[1] is { Expression: ParenthesizedLambdaExpressionSyntax lambda })
                {
                    var returnSyntaxes = new List<ExpressionSyntax>();

                    if (lambda.Body is LiteralExpressionSyntax lit)
                    {
                        returnSyntaxes.Add(lit);
                    }

                    foreach (var n in lambda.Body.DescendantNodes())
                    {
                        if (n is ReturnStatementSyntax r)
                        {
                            returnSyntaxes.Add(r.Expression);
                        }
                    }

                    MapActions.Add((mapActionCall, lambda, lambda.ParameterList.Parameters.Select(p => p.Type).ToArray(), returnSyntaxes.ToArray()));
                }
            }
        }
    }
}
