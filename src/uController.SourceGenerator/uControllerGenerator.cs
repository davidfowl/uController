using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Internal;
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

            var endpointMetadataProviderType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.Metadata.IEndpointMetadataProvider");
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

                                return si.Symbol as IMethodSymbol;
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
                                        Value: var value
                                    }
                                })
                            {
                                return ResolveRoutePattern(value);
                            }
                        }

                        return null;
                    }

                    return expression switch
                    {
                        LiteralExpressionSyntax literal => literal.Token.ValueText,
                        IdentifierNameSyntax id => ResolveIdentifier(id),
                        MemberAccessExpressionSyntax member => ResolveRoutePattern(member.Name),
                        _ => null
                    };
                }

                static bool ShouldDisableInferredBodyForMethod(string method) =>
                    // GET, DELETE, HEAD, CONNECT, TRACE, and OPTIONS normally do not contain bodies
                    method.Equals("MapGet", StringComparison.Ordinal) ||
                    method.Equals("MapDelete", StringComparison.Ordinal) ||
                    method.Equals("MapConnect", StringComparison.Ordinal);

                var methodModel = new MethodModel
                {
                    UniqueName = "RequestHandler",
                    MethodInfo = new MethodInfoWrapper(method, metadataLoadContext),
                    RoutePattern = RoutePattern.Parse(ResolveRoutePattern(routePattern.Expression)),
                    DisableInferBodyFromParameters = ShouldDisableInferredBodyForMethod(callName)
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
                        Method = methodModel,
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
                        if (pattern.HasParameter(parameter.Name))
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

                if (gen.FromBodyTypes.Count > 1)
                {
                    var otherLocations = gen.FromBodyTypes.Select(p => p.ParameterSymbol.DeclaringSyntaxReferences[0].GetSyntax().GetLastToken());
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MultipleParametersConsumingBody, invocation.GetLocation(), otherLocations));
                }

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

                var populateMetadata = new StringBuilder();
                Type returnType = methodModel.MethodInfo.ReturnType;

                if (AwaitableInfo.IsTypeAwaitable(returnType, out var awaitableInfo))
                {
                    returnType = awaitableInfo.ResultType;
                }

                if (returnType.Equals(typeof(void)))
                {
                    // Don't add metadata
                }
                else if (metadataLoadContext.Resolve<IEndpointMetadataProvider>().IsAssignableFrom(returnType))
                {
                    // TODO: Result<T> internally uses reflection to call this method on it's generic args conditionally
                    // we can avoid that reflection here.

                    // TODO: Enable this when we stop calling RDF
                    // Static abstract call
                    // populateMetadata.AppendLine($@"PopulateMetadata<{returnType}>(del.Method, builder);");
                }
                else if (returnType.Equals(typeof(string)))
                {
                    // Add string plaintext
                }
                else
                {
                    // Add JSON
                }

                // Generate code here for this thunk
                thunks.Append($@"            map[(@""{invocation.SyntaxTree.FilePath}"", {lineNumber})] = (
           (del, builder) => 
            {{
                {populateMetadata.ToString().TrimEnd()}
            }}, 
           (del, builder) => 
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
            }});

");

                if (!formattedTypes.Add(fullDelegateType))
                {
                    continue;
                }

                var text = @$"        internal static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder {callName}(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, string pattern, {fullDelegateType} handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = """", [System.Runtime.CompilerServices.CallerLineNumber]int lineNumber = 0)
        {{
            return MapCore(routes, pattern, handler, static (r, p, h) => r.{callName}(p, h), filePath, lineNumber);
        }}

";
                sb.Append(text);
            }

            thunks.AppendLine("        }");

            var mapActionsText = $@"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{{
    delegate void MetadataPopulator(System.Delegate handler, Microsoft.AspNetCore.Builder.EndpointBuilder builder);
    delegate Microsoft.AspNetCore.Http.RequestDelegate RequestDelegateFactoryFunc(System.Delegate handler, Microsoft.AspNetCore.Builder.EndpointBuilder builder);

    public static class MapActionsExtensions
    {{
        private static readonly System.Collections.Generic.Dictionary<(string, int), (MetadataPopulator, RequestDelegateFactoryFunc)> map = new();
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
            var (populate, factory) = map[(filePath, lineNumber)];
            var conventionBuilder = mapper(routes, pattern, handler);

            conventionBuilder.Add(e =>
            {{
                populate(handler, e);
            }});

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

        private static void PopulateMetadata<T>(System.Reflection.MethodInfo method, EndpointBuilder builder) where T : Microsoft.AspNetCore.Http.Metadata.IEndpointMetadataProvider
        {{
            T.PopulateMetadata(method, builder);
        }}

        private static Task ExecuteObjectResult(object obj, HttpContext httpContext)
        {{
            if (obj is IResult r)
            {{
                return r.ExecuteAsync(httpContext);
            }}
            else if (obj is string s)
            {{
                return httpContext.Response.WriteAsync(s);
            }}
            else
            {{
                return httpContext.Response.WriteAsJsonAsync(obj);
            }}
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

    class RoutePattern
    {
        private static readonly char[] Slash = new[] { '/' };

        public string Pattern { get; }

        private string[] _parameterNames;

        public RoutePattern(string pattern, string[] parameterNames)
        {
            Pattern = pattern;
            _parameterNames = parameterNames;
        }

        public bool HasParameter(string name) => _parameterNames.Contains(name);

        public override string ToString() => Pattern;

        public static RoutePattern Parse(string pattern)
        {
            if (pattern is null)
            {
                return null;
            }

            var segments = pattern.Split(Slash, StringSplitOptions.RemoveEmptyEntries);

            List<string> parameters = null;
            foreach (var s in segments)
            {
                // Ignore complex segments and escaping

                var start = s.IndexOf('{');
                if (start != -1)
                {
                    var end = s.IndexOf('}', start + 1);

                    if (end == -1)
                    {
                        continue;
                    }

                    var p = s.Substring(start + 1, end - start - 1);
                    var constraintToken = p.IndexOf(':');

                    if (constraintToken != -1)
                    {
                        // Remove the constraint
                        p = p.Substring(0, constraintToken);
                    }

                    parameters ??= new();
                    parameters.Add(p);
                }
            }

            return new RoutePattern(pattern, parameters?.ToArray() ?? Array.Empty<string>());
        }
    }

    class Diagnostics
    {
        public static readonly DiagnosticDescriptor UnknownDelegateType = new DiagnosticDescriptor("MIN001", "DelegateTypeUnknown", "Unable to determine the parameter and return types from expression \"{0}\"", "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnableToResolveParameter = new DiagnosticDescriptor("MIN002", "ParameterSourceUnknown", "Unable to resolve \"{0}\", consider adding [FromXX] attributes to disambiguate the parameter source", "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnableToResolveTryParseForType = new DiagnosticDescriptor("MIN003", "MissingTryParseForType", "Unable to find a static {0}.TryParse(string, out {0}) implementation", "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnableToResolveRoutePattern = new DiagnosticDescriptor("MIN004", "RoutePatternUnknown", "Unable to detect route pattern, consider adding [FromRoute] on parameters to disambigute between route and querystring values", "Usage", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MultipleParametersConsumingBody = new DiagnosticDescriptor("MIN005", "MultipleParametersFromBody", "Detecting multiple parameters that attempt to read from the body, consider adding [FromXX] attributes to disambiguate the parameter source", "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);
    }
}
