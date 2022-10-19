using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml.Linq;
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

            // Debugger.Launch();

            var metadataLoadContext = new MetadataLoadContext(context.Compilation);
            var assembly = metadataLoadContext.MainAssembly;
            var uControllerAssembly = metadataLoadContext.LoadFromAssemblyName("uController");

            var models = new List<HttpModel>();

            var endpointRouteBuilderType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Routing.IEndpointRouteBuilder");

            // Old codegen
            //foreach (var (memberAccess, handlerType) in receiver.MapHandlers)
            //{
            //    var semanticModel = context.Compilation.GetSemanticModel(memberAccess.Expression.SyntaxTree);
            //    var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);

            //    if (!typeInfo.Type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, endpointRouteBuilderType)))
            //    {
            //        continue;
            //    }

            //    semanticModel = context.Compilation.GetSemanticModel(handlerType.SyntaxTree);
            //    typeInfo = semanticModel.GetTypeInfo(handlerType);

            //    var type = assembly.GetType(typeInfo.Type.ToDisplayString());
            //    var model = HttpModel.FromType(type, uControllerAssembly);
            //    models.Add(model);
            //}

            int number = 0;
            var sb = new StringBuilder();
            var thunks = new StringBuilder();
            var formattedTypes = new HashSet<string>();

            thunks.AppendLine(@$"        static MapActionsExtensions()");
            thunks.AppendLine("        {");

            foreach (var (invocation, argument, callName) in receiver.MapActions)
            {
                var types = new List<string>();
                IMethodSymbol method = default;
                var semanticModel = context.Compilation.GetSemanticModel(invocation.SyntaxTree);

                var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
                var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);

                if (!typeInfo.Type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, endpointRouteBuilderType)))
                {
                    continue;
                }
                var routePattern = invocation.ArgumentList.Arguments[1];

                switch (argument)
                {
                    case IdentifierNameSyntax identifierName:
                        {
                            var si = semanticModel.GetSymbolInfo(identifierName);
                            if (si.CandidateReason == CandidateReason.OverloadResolutionFailure)
                            {
                                // We need to generate the method
                                method = si.CandidateSymbols.SingleOrDefault() as IMethodSymbol;
                            }
                        }
                        break;
                    case ParenthesizedLambdaExpressionSyntax lambda:
                        {
                            var si = semanticModel.GetSymbolInfo(lambda);
                            method = si.Symbol as IMethodSymbol;
                        }
                        break;
                    default:
                        continue;
                }

                if (method == null)
                {
                    continue;
                }

                foreach (var p in method.Parameters)
                {
                    types.Add(p.Type.ToDisplayString());
                }

                types.Add(method.ReturnType.ToDisplayString());

                //var mi = new MethodInfoWrapper(method, metadataLoadContext);
                // var parameters = mi.GetParameters();

                var gen = new MinimalCodeGenerator(metadataLoadContext);

                for (int i = 0; i < 4; i++)
                {
                    gen.Indent();
                }

                var methodModel = new MethodModel
                {
                    UniqueName = "RequestHandler",
                    MethodInfo = new MethodInfoWrapper(method, metadataLoadContext)
                };

                var mvcAssembly = metadataLoadContext.LoadFromAssemblyName("Microsoft.AspNetCore.Mvc.Core");
                var fromQueryAttributeType = mvcAssembly.GetType("Microsoft.AspNetCore.Mvc.FromQueryAttribute");
                var fromRouteAttributeType = mvcAssembly.GetType("Microsoft.AspNetCore.Mvc.FromRouteAttribute");
                var fromHeaderAttributeType = mvcAssembly.GetType("Microsoft.AspNetCore.Mvc.FromHeaderAttribute");
                var fromFormAttributeType = mvcAssembly.GetType("Microsoft.AspNetCore.Mvc.FromFormAttribute");
                var fromBodyAttributeType = mvcAssembly.GetType("Microsoft.AspNetCore.Mvc.FromBodyAttribute");
                var fromServicesAttributeType = mvcAssembly.GetType("Microsoft.AspNetCore.Mvc.FromServicesAttribute");

                foreach (var parameter in methodModel.MethodInfo.GetParameters())
                {
                    var fromQuery = parameter.GetCustomAttributeData(fromQueryAttributeType);
                    var fromHeader = parameter.GetCustomAttributeData(fromHeaderAttributeType);
                    var fromForm = parameter.GetCustomAttributeData(fromFormAttributeType);
                    var fromBody = parameter.GetCustomAttributeData(fromBodyAttributeType);
                    var fromRoute = parameter.GetCustomAttributeData(fromRouteAttributeType);
                    var fromService = parameter.GetCustomAttributeData(fromServicesAttributeType);

                    methodModel.Parameters.Add(new ParameterModel
                    {
                        Name = parameter.Name,
                        ParameterType = parameter.ParameterType,
                        FromQuery = fromQuery == null ? null : fromQuery?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromHeader = fromHeader == null ? null : fromHeader?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromForm = fromForm == null ? null : fromForm?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromRoute = fromRoute == null ? null : fromRoute?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromBody = fromBody != null,
                        FromServices = fromService != null
                    });
                }

                gen.Generate(methodModel);
                var formattedTypeArgs = string.Join(",", types);

                formattedTypeArgs = method.ReturnsVoid ? string.Join(",", types.Take(types.Count - 1)) : formattedTypeArgs;
                var delegateType = method.ReturnsVoid ? "System.Action" : "System.Func";

                FileLinePositionSpan span = invocation.SyntaxTree.GetLineSpan(invocation.Span);
                int lineNumber = span.StartLinePosition.Line + 1;
                // Generate code here for this thunk
                thunks.Append($@"            map[(@""{invocation.SyntaxTree.FilePath}"", {lineNumber})] = (del, builder) => 
            {{
                var handler = ({delegateType}<{formattedTypeArgs}>)del;
{gen}
                return RequestHandler;
            }};

");

                if (!formattedTypes.Add(formattedTypeArgs))
                {
                    continue;
                }

                var text = @$"        internal static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder {callName}(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, string pattern, {delegateType}<{formattedTypeArgs}> handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = """", [System.Runtime.CompilerServices.CallerLineNumber]int lineNumber = 0)
        {{
            var factory = map[(filePath, lineNumber)];
            var conventionBuilder = routes.{callName}(pattern, (System.Delegate)handler);
            conventionBuilder.Add(e =>
            {{
                e.RequestDelegate = factory(handler, e);
            }});

            return conventionBuilder;
        }}
";
                sb.Append(text);
                number++;
            }

            thunks.AppendLine("        }");

            var mapActionsText = $@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{{
    delegate Microsoft.AspNetCore.Http.RequestDelegate RequestDelegateFactoryFunc(System.Delegate handler, Microsoft.AspNetCore.Builder.EndpointBuilder builder);

    public static class MapActionsExtensions
    {{
        private static readonly System.Collections.Generic.Dictionary<(string, int), RequestDelegateFactoryFunc> map = new();
{thunks}
{sb}
    }}
}}";
            if (sb.Length > 0)
            {
                context.AddSource($"MapExtensions", SourceText.From(mapActionsText, Encoding.UTF8));
            }

            // Old source generator
            //foreach (var model in models)
            //{
            //    var gen = new CodeGenerator(model, metadataLoadContext);
            //    var rawSource = gen.Generate();
            //    var sourceText = SourceText.From(rawSource, Encoding.UTF8);

            //    // For debugging
            //    //var comp = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(sourceText));
            //    //var diagnosrics = comp.GetDiagnostics();

            //    context.AddSource(model.HandlerType.Name + "RouteExtensions", sourceText);

            //    //if (gen.FromBodyTypes.Any())
            //    //{
            //    //    var jsonGenerator = new JsonCodeGenerator(metadataLoadContext, model.HandlerType.Namespace);
            //    //    var generatedConverters = jsonGenerator.Generate(gen.FromBodyTypes, out var helperSource);
            //    //}
            //}
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        private class SyntaxReceiver : ISyntaxReceiver
        {
            private static readonly string[] KnownMethods = new[]
            {
                "MapGet",
                "MapPost",
                "MapPut",
                "MapDelete",
                "MapPatch",
                "Map",
            };
            public List<(MemberAccessExpressionSyntax, TypeSyntax)> MapHandlers { get; } = new();

            public List<(InvocationExpressionSyntax, ExpressionSyntax, string)> MapActions { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is MemberAccessExpressionSyntax
                    {
                        Name: GenericNameSyntax
                        {
                            TypeArgumentList:
                            {
                                Arguments: { Count: 1 } arguments
                            },
                            Identifier:
                            {
                                ValueText: "MapHttpHandler"
                            }
                        }
                    } mapHandlerCall)
                {
                    MapHandlers.Add((mapHandlerCall, arguments[0]));
                }

                if (syntaxNode is InvocationExpressionSyntax
                    {
                        Expression: MemberAccessExpressionSyntax
                        {
                            Name: IdentifierNameSyntax
                            {
                                Identifier: { ValueText: var method }
                            }
                        },
                        ArgumentList: { Arguments: { Count: 2 } args }
                    } mapActionCall && KnownMethods.Contains(method))
                {
                    MapActions.Add((mapActionCall, args[1].Expression, method));
                }
            }
        }
    }
}
