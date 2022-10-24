using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
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
        private static readonly Dictionary<string, string> MethodDescriptions = new()
        {
            ["MapGet"] = "HTTP GET",
            ["MapPost"] = "HTTP POST",
            ["MapPut"] = "HTTP PUT",
            ["MapDelete"] = "HTTP DELETE",
            ["MapPatch"] = "HTTP PATCH",
            ["Map"] = "HTTP",
            ["MapFallback"] = "HTTP",
        };

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            {
                // nothing to do yet
                return;
            }

            if (Environment.GetEnvironmentVariable("UCONTROLLER_DEBUG") == "1")
            {
                System.Diagnostics.Debugger.Launch();
            }

            //while (!System.Diagnostics.Debugger.IsAttached)
            //{
            //    System.Threading.Thread.Sleep(1000);
            //}
            // System.Diagnostics.Debugger.Launch();

            var metadataLoadContext = new MetadataLoadContext(context.Compilation);

            var fromQueryAttributeType = metadataLoadContext.Resolve<FromQueryAttribute>();
            var fromRouteAttributeType = metadataLoadContext.Resolve<FromRouteAttribute>();
            var fromHeaderAttributeType = metadataLoadContext.Resolve<FromHeaderAttribute>();
            var fromFormAttributeType = metadataLoadContext.Resolve<FromFormAttribute>();
            var fromBodyAttributeType = metadataLoadContext.Resolve<FromBodyAttribute>();
            var fromServicesAttributeType = metadataLoadContext.Resolve<FromServicesAttribute>();
            var endpointMetadataProviderType = metadataLoadContext.Resolve<IEndpointMetadataProvider>();
            var endpointRouteBuilderType = metadataLoadContext.Resolve<IEndpointRouteBuilder>();
            var delegateMetadataType = metadataLoadContext.Resolve<Delegate>();

            var sb = new StringBuilder();
            var thunks = new StringBuilder();
            var generatedMethodSignatures = new HashSet<string>();

            thunks.AppendLine(@$"        static MapActionsExtensions()");
            thunks.AppendLine("        {");

            foreach (var (invocation, argument, callName) in receiver.MapActions)
            {
                var semanticModel = context.Compilation.GetSemanticModel(invocation.SyntaxTree);

                var mapMethodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

                if (mapMethodSymbol is { Parameters: { Length: 2 } parameters } &&
                    delegateMetadataType.Equals(parameters[1].Type) &&
                    endpointRouteBuilderType.Equals(mapMethodSymbol.ReceiverType))
                {
                    // We only want to generate overloads for calls that have a Delegate parameter
                }
                else
                {
                    continue;
                }

                static IMethodSymbol ResolveMethod(SemanticModel semanticModel, ExpressionSyntax expression)
                {
                    switch (expression)
                    {
                        case ParenthesizedLambdaExpressionSyntax:
                        case MemberAccessExpressionSyntax:
                        case IdentifierNameSyntax:
                            {
                                var si = semanticModel.GetSymbolInfo(expression);

                                if (si.Symbol is IMethodSymbol methodSymbol)
                                {
                                    return methodSymbol;
                                }

                                IMethodSymbol method = null;

                                if (si.CandidateReason == CandidateReason.OverloadResolutionFailure)
                                {
                                    method = si.CandidateSymbols.Length == 1 ? si.CandidateSymbols[0] as IMethodSymbol : null;
                                }

                                if (method is null)
                                {
                                    var isReadOnly = si.Symbol switch
                                    {
                                        IFieldSymbol fieldSymbol => fieldSymbol.IsReadOnly,
                                        ILocalSymbol localSymbol => localSymbol.IsConst,
                                        _ => false
                                    };

                                    // If the reference to fields or locals are not const the we
                                    // can't assume it won't be re-assigned
                                    if (!isReadOnly)
                                    {
                                        return null;
                                    }

                                    foreach (var syntaxReference in si.Symbol.DeclaringSyntaxReferences)
                                    {
                                        var syn = syntaxReference.GetSyntax();

                                        if (syn is VariableDeclaratorSyntax
                                            {
                                                Initializer:
                                                {
                                                    Value: var expr
                                                }
                                            })
                                        {
                                            method = ResolveMethod(semanticModel, expr);

                                            if (method is not null)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }

                                return method;
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

                string ResolveRoutePattern(ExpressionSyntax expression)
                {
                    string ResolveIdentifier(IdentifierNameSyntax id)
                    {
                        var symbol = semanticModel.GetSymbolInfo(id).Symbol;
                        if (symbol is null)
                        {
                            return null;
                        }

                        var isReadOnly = symbol switch
                        {
                            IFieldSymbol fieldSymbol => fieldSymbol.IsReadOnly,
                            ILocalSymbol localSymbol => localSymbol.IsConst,
                            _ => false
                        };

                        // If the reference to fields or locals are not const the we
                        // can't assume it won't be re-assigned
                        if (!isReadOnly)
                        {
                            return null;
                        }

                        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
                        {
                            var syntax = syntaxReference.GetSyntax();

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

                var routePattern = invocation.ArgumentList.Arguments[0];

                var methodModel = new MethodModel
                {
                    UniqueName = "RequestHandler",
                    MethodInfo = method.AsMethodInfo(metadataLoadContext),
                    RoutePattern = RoutePattern.Parse(ResolveRoutePattern(routePattern.Expression))
                };

                var hasAmbiguousParameterWithoutRoute = false;
                var parameterIndex = 0;

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
                        ParameterSymbol = parameter.GetParameterSymbol(),
                        Name = parameter.Name,
                        GeneratedName = "arg_" + parameter.Name.Replace("_", "__"),
                        ParameterType = parameter.ParameterType,
                        FromQuery = fromQuery == null ? null : fromQuery?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromHeader = fromHeader == null ? null : fromHeader?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromForm = fromForm == null ? null : fromForm?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromRoute = fromRoute == null ? null : fromRoute?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromBody = fromBody != null,
                        FromServices = fromService != null,
                        Index = parameterIndex
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
                    parameterIndex++;
                }

                if (hasAmbiguousParameterWithoutRoute)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnableToResolveRoutePattern, routePattern.GetLocation()));
                }

                var codeGenerator = new MinimalCodeGenerator(metadataLoadContext);

                for (int i = 0; i < 4; i++)
                {
                    codeGenerator.Indent();
                }

                codeGenerator.Generate(methodModel);

                if (codeGenerator.BodyParameters.Count > 1)
                {
                    var mainLocation = (method.DeclaringSyntaxReferences[0].GetSyntax() switch
                    {
                        MethodDeclarationSyntax methodDeclarationSyntax => methodDeclarationSyntax.Identifier.GetLocation(),
                        LocalFunctionStatementSyntax localFunctionStatementSyntax => localFunctionStatementSyntax.Identifier.GetLocation(),
                        var expr => expr.GetLocation()
                    });

                    var otherLocations = new List<Location>();

                    foreach (var p in codeGenerator.BodyParameters)
                    {
                        foreach (var syntaxReference in p.ParameterSymbol.DeclaringSyntaxReferences)
                        {
                            otherLocations.Add(syntaxReference.GetSyntax().GetLocation());
                        }

                        p.Unresovled = true;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MultipleParametersConsumingBody, mainLocation, otherLocations));
                }

                var preReq = new StringBuilder();
                var runtimeChecks = new StringBuilder();
                var generatedRoutePatternVars = false;
                var generatedBodyOrService = false;
                var generatedParameterInfos = false;

                foreach (var p in methodModel.Parameters)
                {
                    if (p.Unresovled)
                    {
                        var loc = p.ParameterSymbol.DeclaringSyntaxReferences[0].GetSyntax().GetLocation();
                        if (p.HasBindingSource && !p.FromBody)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnableToResolveTryParseForType, loc, p.ParameterType.FullName));
                        }
                        else
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnableToResolveParameter, loc, p.Name));
                        }
                    }

                    if (p.QueryOrRoute)
                    {
                        if (!generatedRoutePatternVars)
                        {
                            generatedRoutePatternVars = true;
                            preReq.AppendLine("                var routePattern = (builder as RouteEndpointBuilder)?.RoutePattern;");
                        }

                        runtimeChecks.AppendLine($@"                System.Func<HttpContext, string, Microsoft.Extensions.Primitives.StringValues> {p.GeneratedName}RouteOrQueryResolver = routePattern?.GetParameter(""{p.Name}"") is null ? ResolveByQuery : ResolveByRoute;");
                    }

                    if (p.BodyOrService)
                    {
                        if (!generatedBodyOrService)
                        {
                            generatedBodyOrService = true;
                            preReq.AppendLine("                var ispis = builder.ApplicationServices.GetService<IServiceProviderIsService>();");
                        }

                        runtimeChecks.AppendLine($@"                System.Func<HttpContext, System.Threading.Tasks.ValueTask<{p.ParameterType}>> {p.GeneratedName}ServiceOrBodyResolver = (ispis?.IsService(typeof({p.ParameterType})) ?? false) ? ResolveService<{p.ParameterSymbol}> : ResolveBody<{p.ParameterType}>;");
                    }

                    if (p.RequiresParameterInfo)
                    {
                        if (!generatedParameterInfos)
                        {
                            generatedParameterInfos = true;
                            preReq.AppendLine("                var parameterInfos = del.Method.GetParameters();");
                        }

                        runtimeChecks.AppendLine($@"                var {p.GeneratedName}ParameterInfo = parameterInfos[{p.Index}];");
                    }
                }

                // List of parameters and return type
                var types = new List<string>();
                foreach (var p in method.Parameters)
                {
                    types.Add(p.Type.ToDisplayString());
                }

                types.Add(method.ReturnType.ToDisplayString());

                var formattedTypeArgs = string.Join(", ", types);

                formattedTypeArgs = method.ReturnsVoid ? string.Join(", ", types.Take(types.Count - 1)) : formattedTypeArgs;
                var delegateType = method.ReturnsVoid ? "System.Action" : "System.Func";
                var fullDelegateType = formattedTypeArgs.Length == 0 ? delegateType : $"{delegateType}<{formattedTypeArgs}>";

                // REVIEW: Figure out why open generics don't work
                //var formattedOpenGenericArgs = string.Join(", ", (method.ReturnsVoid ? types.Take(types.Count - 1) : types).Select((t, i) => $"T{i}"));
                //formattedOpenGenericArgs = formattedOpenGenericArgs.Length == 0 ? formattedOpenGenericArgs : $"<{formattedOpenGenericArgs}>";
                //var openGenericType = formattedOpenGenericArgs.Length == 0 ? delegateType : $"{delegateType}{formattedOpenGenericArgs}";

                var filterArgumentString = string.Join(", ", types.Take(types.Count - 1).Select((t, i) => $"ic.GetArgument<{t}>({i})"));

                // Get the source location (file and line number)
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
                else if (endpointMetadataProviderType is not null && endpointMetadataProviderType.IsAssignableFrom(returnType))
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
{preReq}{runtimeChecks.ToString().TrimEnd()}
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

{codeGenerator}
                return filteredInvocation is null ? RequestHandler : RequestHandlerFiltered;
            }});

");

                if (!generatedMethodSignatures.Add($"{callName}_{fullDelegateType}"))
                {
                    continue;
                }

                MethodDescriptions.TryGetValue(callName, out var description);

                var text = @$"        /// <summary>
        /// Adds a <see cref=""RouteEndpoint""/> to the <see cref=""IEndpointRouteBuilder""/> that matches {description ?? "HTTP"} requests
        /// for the specified pattern.
        /// </summary>
        /// <param name=""endpoints"">The <see cref=""IEndpointRouteBuilder""/> to add the route to.</param>
        /// <param name=""pattern"">The route pattern.</param>
        /// <param name=""handler"">The delegate executed when the endpoint is matched.</param>
        /// <returns>A <see cref=""RouteHandlerBuilder""/> that can be used to further customize the endpoint.</returns>    
        internal static Microsoft.AspNetCore.Builder.RouteHandlerBuilder {callName}(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, {fullDelegateType} handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = """", [System.Runtime.CompilerServices.CallerLineNumber]int lineNumber = 0)
        {{
            return MapCore(endpoints, pattern, handler, static (r, p, h) => r.{callName}(p, h), filePath, lineNumber);
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

#if NET7_0_OR_GREATER
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

        private static Microsoft.Extensions.Primitives.StringValues ResolveByQuery(HttpContext context, string key) => context.Request.Query[key];
        private static Microsoft.Extensions.Primitives.StringValues ResolveByRoute(HttpContext context, string key) => context.Request.RouteValues[key]?.ToString();
        private static ValueTask<T> ResolveService<T>(HttpContext context) => new ValueTask<T>(context.RequestServices.GetRequiredService<T>());
        private static ValueTask<T> ResolveBody<T>(HttpContext context) => context.Request.ReadFromJsonAsync<T>();
    }}
}}
#endif
";
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
                "MapFallback", // This doesn't work yet because it doesn't have a path
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
        public static readonly DiagnosticDescriptor UnknownDelegateType = new DiagnosticDescriptor("MIN001", "DelegateTypeUnknown", "Unable to determine the parameter and return types from expression \"{0}\", only method groups, lambda expressions or readonly fields/variables are allowed", "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnableToResolveParameter = new DiagnosticDescriptor("MIN002", "ParameterSourceUnknown", "Unable to resolve \"{0}\", consider adding [FromXX] attributes to disambiguate the parameter source", "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnableToResolveTryParseForType = new DiagnosticDescriptor("MIN003", "MissingTryParseForType", "Unable to find a static {0}.TryParse(string, out {0}) implementation", "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnableToResolveRoutePattern = new DiagnosticDescriptor("MIN004", "RoutePatternUnknown", "Unable to detect route pattern, consider adding [FromRoute] on parameters to disambigute between route and querystring values", "Usage", DiagnosticSeverity.Info, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MultipleParametersConsumingBody = new DiagnosticDescriptor("MIN005", "MultipleParametersFromBody", "Detecting multiple parameters that attempt to read from the body, consider adding [FromXX] attributes to disambiguate the parameter source", "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);
    }
}
