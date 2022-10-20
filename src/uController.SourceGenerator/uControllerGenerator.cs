﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

            // System.Diagnostics.Debugger.Launch();

            var metadataLoadContext = new MetadataLoadContext(context.Compilation);

            var endpointRouteBuilderType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Routing.IEndpointRouteBuilder");
            var sb = new StringBuilder();
            var thunks = new StringBuilder();
            var formattedTypes = new HashSet<string>();

            thunks.AppendLine(@$"        static MapActionsExtensions()");
            thunks.AppendLine("        {");

            foreach (var (invocation, argument, callName) in receiver.MapActions)
            {
                var types = new List<string>();
                var semanticModel = context.Compilation.GetSemanticModel(invocation.SyntaxTree);

                var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
                var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);

                if (!SymbolEqualityComparer.Default.Equals(typeInfo.Type, endpointRouteBuilderType) &&
                    !typeInfo.Type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, endpointRouteBuilderType)))
                {
                    continue;
                }
                var routePattern = invocation.ArgumentList.Arguments[0];

                static IMethodSymbol ResolveMethod(SemanticModel semanticModel, ExpressionSyntax expression)
                {
                    switch (expression)
                    {
                        case IdentifierNameSyntax identifierName:
                            {
                                IMethodSymbol method = null;

                                var si = semanticModel.GetSymbolInfo(identifierName);
                                if (si.CandidateReason == CandidateReason.OverloadResolutionFailure)
                                {
                                    // We need to generate the method
                                    method = si.CandidateSymbols.SingleOrDefault() as IMethodSymbol;
                                }

                                if (method is null)
                                {
                                    var syn = si.Symbol.DeclaringSyntaxReferences[0].GetSyntax();

                                    if (syn is VariableDeclaratorSyntax
                                        {
                                            Initializer:
                                            {
                                                Value: var expr
                                            }
                                        })
                                    {
                                        method = ResolveMethod(semanticModel, expr);
                                    }
                                }

                                return method;
                            }
                        case ParenthesizedLambdaExpressionSyntax lambda:
                            {
                                var si = semanticModel.GetSymbolInfo(lambda);
                                return si.Symbol as IMethodSymbol;
                            }
                        case MemberAccessExpressionSyntax memberAccessExpression:
                            {
                                var si = semanticModel.GetSymbolInfo(memberAccessExpression);
                                if (si.CandidateReason == CandidateReason.OverloadResolutionFailure)
                                {
                                    // We need to generate the method
                                    return si.CandidateSymbols.SingleOrDefault() as IMethodSymbol;
                                }

                                return null;
                            }
                        default:
                            return null;
                    }
                }

                var method = ResolveMethod(semanticModel, argument);

                if (method == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnknownDelegateType, argument.GetLocation(), argument.ToFullString()));
                    continue;
                }

                foreach (var p in method.Parameters)
                {
                    types.Add(p.Type.ToDisplayString());
                }

                types.Add(method.ReturnType.ToDisplayString());

                var gen = new MinimalCodeGenerator(metadataLoadContext);

                for (int i = 0; i < 4; i++)
                {
                    gen.Indent();
                }

                string ResolveRoutePattern(ExpressionSyntax expression)
                {
                    string ResolveIdentifier(IdentifierNameSyntax id)
                    {
                        var symbol = semanticModel.GetSymbolInfo(id).Symbol;
                        if (symbol is null)
                        {
                            return null;
                        }

                        foreach (var decl in symbol.DeclaringSyntaxReferences)
                        {
                            var syntax = decl.GetSyntax();

                            if (syntax is VariableDeclaratorSyntax
                                {
                                    Initializer:
                                    {
                                        Value: LiteralExpressionSyntax
                                        {
                                            Token: { ValueText: var text }
                                        }
                                    }
                                })
                            {
                                return text;
                            }
                        }

                        return null;
                    }

                    return expression switch
                    {
                        LiteralExpressionSyntax literal => literal.Token.ValueText,
                        IdentifierNameSyntax id => ResolveIdentifier(id),
                        _ => null
                    };
                }

                var methodModel = new MethodModel
                {
                    UniqueName = "RequestHandler",
                    MethodInfo = new MethodInfoWrapper(method, metadataLoadContext),
                    // TODO: Parse the route pattern here
                    RoutePattern = ResolveRoutePattern(routePattern.Expression)
                };

                var mvcAssembly = metadataLoadContext.LoadFromAssemblyName("Microsoft.AspNetCore.Mvc.Core");
                var fromQueryAttributeType = mvcAssembly.GetType("Microsoft.AspNetCore.Mvc.FromQueryAttribute");
                var fromRouteAttributeType = mvcAssembly.GetType("Microsoft.AspNetCore.Mvc.FromRouteAttribute");
                var fromHeaderAttributeType = mvcAssembly.GetType("Microsoft.AspNetCore.Mvc.FromHeaderAttribute");
                var fromFormAttributeType = mvcAssembly.GetType("Microsoft.AspNetCore.Mvc.FromFormAttribute");
                var fromBodyAttributeType = mvcAssembly.GetType("Microsoft.AspNetCore.Mvc.FromBodyAttribute");
                var fromServicesAttributeType = mvcAssembly.GetType("Microsoft.AspNetCore.Mvc.FromServicesAttribute");

                var hasAmbiguousParameterWithoutRoute = false;

                foreach (var parameter in methodModel.MethodInfo.GetParameters())
                {
                    var fromQuery = parameter.GetCustomAttributeData(fromQueryAttributeType);
                    var fromHeader = parameter.GetCustomAttributeData(fromHeaderAttributeType);
                    var fromForm = parameter.GetCustomAttributeData(fromFormAttributeType);
                    var fromBody = parameter.GetCustomAttributeData(fromBodyAttributeType);
                    var fromRoute = parameter.GetCustomAttributeData(fromRouteAttributeType);
                    var fromService = parameter.GetCustomAttributeData(fromServicesAttributeType);

                    var parameterModel = new ParameterModel
                    {
                        ParameterSymbol = (parameter as ParameterWrapper).ParameterSymbol,
                        Name = parameter.Name,
                        ParameterType = parameter.ParameterType,
                        FromQuery = fromQuery == null ? null : fromQuery?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromHeader = fromHeader == null ? null : fromHeader?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromForm = fromForm == null ? null : fromForm?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromRoute = fromRoute == null ? null : fromRoute?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromBody = fromBody != null,
                        FromServices = fromService != null
                    };

                    if (methodModel.RoutePattern is { } pattern)
                    {
                        if (pattern.Contains($"{{{parameter.Name}}}"))
                        {
                            parameterModel.FromRoute = parameter.Name;
                        }
                    }
                    else if (!parameterModel.HasBindingSource)
                    {
                        hasAmbiguousParameterWithoutRoute = true;
                    }

                    methodModel.Parameters.Add(parameterModel);
                }

                if (hasAmbiguousParameterWithoutRoute)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnableToResolveRoutePattern, routePattern.GetLocation()));
                }

                gen.Generate(methodModel);

                foreach (var p in methodModel.Parameters)
                {
                    if (p.Unresovled)
                    {
                        var loc = p.ParameterSymbol.DeclaringSyntaxReferences[0].GetSyntax().GetLocation();
                        if (p.HasBindingSource)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnableToResolveTryParseForType, loc, p.ParameterType.FullName));
                        }
                        else
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnableToResolveParameter, loc, p.Name));
                        }
                    }
                }

                var formattedTypeArgs = string.Join(", ", types);

                formattedTypeArgs = method.ReturnsVoid ? string.Join(", ", types.Take(types.Count - 1)) : formattedTypeArgs;
                var delegateType = method.ReturnsVoid ? "System.Action" : "System.Func";
                var fullDelegateType = formattedTypeArgs.Length == 0 ? delegateType : $"{delegateType}<{formattedTypeArgs}>";

                var formattedOpenGenericArgs = string.Join(", ", (method.ReturnsVoid ? types.Take(types.Count - 1) : types).Select((t, i) => $"T{i}"));
                formattedOpenGenericArgs = formattedOpenGenericArgs.Length == 0 ? formattedOpenGenericArgs : $"<{formattedOpenGenericArgs}>";
                var openGenericType = formattedOpenGenericArgs.Length == 0 ? delegateType : $"{delegateType}{formattedOpenGenericArgs}";

                var filterArgumentString = string.Join(", ", types.Take(types.Count - 1).Select((t, i) => $"ic.GetArgument<{t}>({i})"));

                var span = invocation.SyntaxTree.GetLineSpan(invocation.Span);
                var lineNumber = span.StartLinePosition.Line + 1;

                var filteredInvocationText = method.ReturnsVoid ?
                    $@"handler({filterArgumentString});
                        return System.Threading.Tasks.ValueTask.FromResult<object>(Results.Empty);" :
                    $@"return System.Threading.Tasks.ValueTask.FromResult<object>(handler({filterArgumentString}));";

                // Generate code here for this thunk
                thunks.Append($@"            map[(@""{invocation.SyntaxTree.FilePath}"", {lineNumber})] = (del, builder) => 
            {{
                var handler = ({fullDelegateType})del;
                EndpointFilterDelegate filteredInvocation = null;

                if (builder.FilterFactories.Count > 0)
                {{
                    filteredInvocation = BuildFilterDelegate(ic => 
                    {{
                        if (ic.HttpContext.Response.StatusCode == 400)
                        {{
                            return System.Threading.Tasks.ValueTask.FromResult<object>(Results.Empty);
                        }}
                        {filteredInvocationText}
                    }},
                    builder,
                    handler.Method);
                }}

{gen}
                return filteredInvocation is null ? RequestHandler : RequestHandlerFiltered;
            }};

");

                if (!formattedTypes.Add(openGenericType))
                {
                    continue;
                }

                var text = @$"        internal static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder {callName}{formattedOpenGenericArgs}(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, string pattern, {openGenericType} handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = """", [System.Runtime.CompilerServices.CallerLineNumber]int lineNumber = 0)
        {{
            return MapCore(routes, pattern, handler, static (r, p, h) => r.{callName}(p, h), filePath, lineNumber);
        }}

";
                sb.Append(text);
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
{sb.ToString().TrimEnd()}

        private static Microsoft.AspNetCore.Builder.RouteHandlerBuilder MapCore(
            this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, 
            string pattern, 
            System.Delegate handler, 
            Func<Microsoft.AspNetCore.Routing.IEndpointRouteBuilder, string, System.Delegate, Microsoft.AspNetCore.Builder.RouteHandlerBuilder> mapper,
            string filePath,
            int lineNumber)
        {{
            var factory = map[(filePath, lineNumber)];
            var conventionBuilder = mapper(routes, pattern, handler);
            conventionBuilder.Finally(e =>
            {{
                e.RequestDelegate = factory(handler, e);
            }});

            return conventionBuilder;
        }}

        private static EndpointFilterDelegate BuildFilterDelegate(EndpointFilterDelegate filteredInvocation, EndpointBuilder builder, System.Reflection.MethodInfo mi)
        {{
            var routeHandlerFilters =  builder.FilterFactories;

            var context0 = new EndpointFilterFactoryContext
            {{
                MethodInfo = mi,
                ApplicationServices = builder.ApplicationServices,
            }};

            var initialFilteredInvocation = filteredInvocation;

            for (var i = routeHandlerFilters.Count - 1; i >= 0; i--)
            {{
                var filterFactory = routeHandlerFilters[i];
                filteredInvocation = filterFactory(context0, filteredInvocation);
            }}

            return filteredInvocation;
        }}
    }}
}}";
            if (sb.Length > 0)
            {
                context.AddSource($"MapExtensions", SourceText.From(mapActionsText, Encoding.UTF8));
            }
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

            public List<(InvocationExpressionSyntax, ExpressionSyntax, string)> MapActions { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
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

    class Diagnostics
    {
        public static readonly DiagnosticDescriptor UnknownDelegateType = new DiagnosticDescriptor("MINIMAL001", "DelegateTypeUnknown", "Unable to infer delegate type from expression \"{0}\"", "5000", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnableToResolveParameter = new DiagnosticDescriptor("MINIMAL002", "ParameterSourceUnknown", "Unable to detect parameter source for \"{0}\", consider adding [FromXX] attributes", "5000", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnableToResolveTryParseForType = new DiagnosticDescriptor("MINIMAL003", "MissingTryParseForType", "Unable to find a static {0}.TryParse(string, out {0}) implementation", "5000", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnableToResolveRoutePattern = new DiagnosticDescriptor("MINIMAL004", "RoutePatternUnknown", "Unable to detect route pattern source, consider adding [FromRoute] on parameters to disambigute between route and querystring values", "5000", DiagnosticSeverity.Warning, isEnabledByDefault: true);
    }
}
