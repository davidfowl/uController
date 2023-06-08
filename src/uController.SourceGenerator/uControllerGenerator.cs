using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Internal;
using Roslyn.Reflection;

namespace uController.SourceGenerator
{
    [Generator]
    public class uControllerGenerator : ISourceGenerator
    {
        private static readonly Dictionary<string, string> MethodDescriptions = new()
        {
            ["MapGet"] = "Get",
            ["MapPost"] = "Post",
            ["MapPut"] = "Put",
            ["MapDelete"] = "Delete",
            ["MapPatch"] = "Patch"
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

            var metadataLoadContext = new MetadataLoadContext(context.Compilation);
            var wellKnownTypes = new WellKnownTypes(metadataLoadContext);

            var sb = new StringBuilder();
            var thunks = new StringBuilder();
            var genericThunks = new StringBuilder();
            var generatedMethodSignatures = new HashSet<string>();

            var knownTypedResultsMethods = wellKnownTypes.TypedResultsType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                         .ToLookup(m => (m.Name, m.IsGenericMethod));

            foreach (var invocation in receiver.MapActions)
            {
                var semanticModel = context.Compilation.GetSemanticModel(invocation.SyntaxTree);

                var operation = semanticModel.GetOperation(invocation);

                if (operation is IInvocationOperation { Arguments: { Length: 3 } parameters } invocationOperation &&
                    wellKnownTypes.DelegateType.Equals(parameters[2].Parameter.Type) &&
                    wellKnownTypes.EndpointRouteBuilderType.Equals(parameters[0].Parameter.Type))
                {
                    // We only want to generate overloads for calls that have a Delegate parameter
                }
                else
                {
                    continue;
                }

                var callName = invocationOperation.TargetMethod.Name;
                var routePatternArgument = invocationOperation.Arguments[1];
                var delegateArgument = invocationOperation.Arguments[2];

                IOperation ResolveDeclarationOperation(ISymbol symbol)
                {
                    foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
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
                            // Use the correct semantic model based on the syntax tree
                            var operation = context.Compilation.GetSemanticModel(syn.SyntaxTree).GetOperation(expr);

                            if (operation is not null)
                            {
                                return operation;
                            }
                        }
                    }

                    return null;
                }

                // Could be rewritten as a while loop
                IMethodSymbol ResolveMethodFromOperation(IOperation operation) => operation switch
                {
                    IArgumentOperation argument => ResolveMethodFromOperation(argument.Value),
                    IConversionOperation conv => ResolveMethodFromOperation(conv.Operand),
                    IDelegateCreationOperation del => ResolveMethodFromOperation(del.Target),
                    IFieldReferenceOperation f when f.Field.IsReadOnly && ResolveDeclarationOperation(f.Field) is IOperation op => ResolveMethodFromOperation(op),
                    IAnonymousFunctionOperation anon => anon.Symbol,
                    ILocalFunctionOperation local => local.Symbol,
                    IMethodReferenceOperation method => method.Method,
                    _ => null
                };

                var method = ResolveMethodFromOperation(delegateArgument);

                if (method == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnknownDelegateType, delegateArgument.Syntax.GetLocation(), delegateArgument.Syntax.ToFullString()));
                    continue;
                }

                object ResolveLiteralOperation(IOperation operation) => operation switch
                {
                    IArgumentOperation argument => ResolveLiteralOperation(argument.Value),
                    ILiteralOperation literal => literal.ConstantValue.Value,
                    ILocalReferenceOperation l when l.Local.IsConst && ResolveDeclarationOperation(l.Local) is IOperation op => ResolveLiteralOperation(op),
                    IFieldReferenceOperation f when f.Field.IsReadOnly && ResolveDeclarationOperation(f.Field) is IOperation op => ResolveLiteralOperation(op),
                    _ => null
                };

                var routePattern = ResolveLiteralOperation(routePatternArgument) as string;

                var methodModel = new MethodModel
                {
                    UniqueName = "RequestHandler",
                    MethodInfo = method.AsMethodInfo(metadataLoadContext),
                    RoutePattern = RoutePattern.Parse(routePattern)
                };

                var parameterIndex = 0;

                foreach (var parameter in methodModel.MethodInfo.GetParameters())
                {
                    var fromQuery = parameter.GetCustomAttributeData(wellKnownTypes.FromQueryMetadataType);
                    var fromHeader = parameter.GetCustomAttributeData(wellKnownTypes.FromHeaderMetadataType);
                    var fromForm = parameter.GetCustomAttributeData(wellKnownTypes.FromFormMetadataType);
                    var fromBody = parameter.GetCustomAttributeData(wellKnownTypes.FromBodyMetadataType);
                    var fromRoute = parameter.GetCustomAttributeData(wellKnownTypes.FromRouteMetadataType);
                    var fromService = parameter.GetCustomAttributeData(wellKnownTypes.FromServicesMetadataType);
                    var asParameters = parameter.GetCustomAttributeData(wellKnownTypes.AsParametersAttributeType);

                    var parameterModel = new ParameterModel
                    {
                        Method = methodModel,
                        ParameterSymbol = parameter.GetParameterSymbol(),
                        ParameterInfo = parameter,
                        Name = parameter.Name,
                        GeneratedName = "arg_" + parameter.Name,
                        ParameterType = parameter.ParameterType,
                        FromQuery = fromQuery == null ? null : fromQuery?.GetNamedArgument<string>("Name") ?? parameter.Name,
                        FromHeader = fromHeader == null ? null : fromHeader?.GetNamedArgument<string>("Name") ?? parameter.Name,
                        FromForm = fromForm == null ? null : fromForm?.GetNamedArgument<string>("Name") ?? parameter.Name,
                        FromRoute = fromRoute == null ? null : fromRoute?.GetNamedArgument<string>("Name") ?? parameter.Name,
                        FromBodyAttributeData = fromBody,
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

                    methodModel.Parameters.Add(parameterModel);
                    parameterIndex++;
                }

                var codeGenerator = new MinimalCodeGenerator(wellKnownTypes);

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

                        runtimeChecks.AppendLine($@"                System.Func<HttpContext, string, Microsoft.Extensions.Primitives.StringValues?> {p.GeneratedName}RouteOrQueryResolver = routePattern?.GetParameter(""{p.Name}"") is null ? ResolveByQuery : ResolveByRoute;");
                    }

                    if (p.BodyOrService)
                    {
                        if (!generatedBodyOrService)
                        {
                            generatedBodyOrService = true;
                            preReq.AppendLine("                var ispis = builder.ApplicationServices.GetService<IServiceProviderIsService>();");
                        }

                        var isOptional = p.ParameterSymbol.IsOptional || p.ParameterSymbol.NullableAnnotation == NullableAnnotation.Annotated;
                        var resolveBody = isOptional ? $"ResolveBodyOptional<{p.ParameterType}>" : $"ResolveBodyRequired<{p.ParameterType}>";

                        runtimeChecks.AppendLine($@"                System.Func<HttpContext, System.Threading.Tasks.ValueTask<{p.ParameterType}>> {p.GeneratedName}ServiceOrBodyResolver = (ispis?.IsService(typeof({p.ParameterType.ToString().Replace("?", string.Empty)})) ?? false) ? ResolveService<{p.ParameterType}>({isOptional.ToString().ToLower()}) : {resolveBody};");
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

                var hasAnonymoysParameterType = false;
                // List of parameters and return type
                var types = new List<string>();
                foreach (var p in method.Parameters)
                {
                    if (p.Type.IsAnonymousType)
                    {
                        var loc = p.DeclaringSyntaxReferences[0].GetSyntax().GetLocation();
                        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.AnonymousTypesAsParametersAreNotSupported, loc));
                    }

                    types.Add(p.Type.ToDisplayString());
                }

                if (hasAnonymoysParameterType)
                {
                    // Don't generate this method
                    continue;
                }

                if (method.ReturnType.IsAnonymousType)
                {
                    types.Add("T");
                }
                else
                {
                    types.Add(method.ReturnType.ToDisplayString());
                }

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
                        return System.Threading.Tasks.ValueTask.FromResult<object?>(Results.Empty);" :
                    $@"return System.Threading.Tasks.ValueTask.FromResult<object?>(handler({filterArgumentString}));";

                var populateMetadata = new StringBuilder();
                var metadataPreReqs = new StringBuilder();
                var generatedMetadataParameterInfo = false;
                var generatedIsServiceProvider = false;

                foreach (var p in methodModel.Parameters)
                {
                    if (wellKnownTypes.IEndpointMetadataProviderType?.IsAssignableFrom(p.ParameterType) is true)
                    {
                        populateMetadata.AppendLine($@"                PopulateMetadata<{p.ParameterType}>(del.Method, builder);");
                    }

                    if (wellKnownTypes.IEndpointParameterMetadataProviderType?.IsAssignableFrom(p.ParameterType) is true)
                    {
                        if (!generatedMetadataParameterInfo)
                        {
                            metadataPreReqs.AppendLine("                var parameterInfos = del.Method.GetParameters();");
                            generatedMetadataParameterInfo = true;
                        }
                        populateMetadata.AppendLine($@"                PopulateMetadata<{p.ParameterType}>(parameterInfos[{p.Index}], builder);");
                    }

                    if (p.FromBody)
                    {
                        var acceptsType = $"{p.ParameterType}".Trim('?');
                        populateMetadata.AppendLine($@"                builder.Metadata.Add(new AcceptsTypeMetadata(typeof({acceptsType}), true, new[] {{ ""application/json"" }}));");
                    }
                    else if (p.BodyOrService)
                    {
                        var acceptsType = $"{p.ParameterType}".Trim('?');
                        if (!generatedIsServiceProvider)
                        {
                            metadataPreReqs.AppendLine($@"                var ispis = builder.ApplicationServices.GetService<IServiceProviderIsService>();");
                            generatedIsServiceProvider = true;
                        }
                        populateMetadata.AppendLine($@"                if ((ispis?.IsService(typeof({acceptsType})) ?? false) == false) builder.Metadata.Add(new AcceptsTypeMetadata(typeof({acceptsType}), true, new[] {{ ""application/json"" }}));");
                    }

                    if (p.ReadFromForm)
                    {
                        populateMetadata.AppendLine($@"                builder.Metadata.Add(new AcceptsTypeMetadata(typeof({p.ParameterType}), true, new[] {{ ""multipart/form-data"" }}));");  
                    }
                }

                void AnalyzeResultTypesForIResultMethods(IMethodSymbol method, Type returnType)
                {
                    if (!wellKnownTypes.IResultType.Equals(returnType))
                    {
                        // Don't bother if we're not looking at an IResult returning method
                        return;
                    }

                    foreach (var reference in method.DeclaringSyntaxReferences)
                    {
                        var syntax = reference.GetSyntax();

                        var operation = semanticModel.GetOperation(syntax);
                        // TODO: This needs a real detailed analysis working from return expressions, this is a bit of a hack right now
                        // looking for *any* results call in this method. This should find all exit points from a method
                        // and start from there.
                        foreach (var op in operation.Descendants().OfType<IInvocationOperation>())
                        {
                            var resultsMethod = op.TargetMethod;

                            if (resultsMethod.IsStatic &&
                                wellKnownTypes.ResultsType.Equals(resultsMethod.ContainingType))
                            {
                                if (resultsMethod.Name == "StatusCode")
                                {
                                    // Try to resolve the status code statically
                                    var literalExpression = ResolveLiteralOperation(op.Arguments[0].Value);

                                    if (literalExpression is not null)
                                    {
                                        populateMetadata.AppendLine($@"                builder.Metadata.Add(ResponseTypeMetadata.Create({literalExpression}));");
                                    }

                                }
                                else
                                {
                                    var candidate = knownTypedResultsMethods[(resultsMethod.Name, resultsMethod.IsGenericMethod)].FirstOrDefault();

                                    if (candidate is not null)
                                    {
                                        if (candidate.IsGenericMethod)
                                        {
                                            candidate = candidate.MakeGenericMethod(resultsMethod.AsMethodInfo(metadataLoadContext).GetGenericArguments());
                                        }
                                    }

                                    if (wellKnownTypes.IEndpointMetadataProviderType?.IsAssignableFrom(candidate.ReturnType) == true)
                                    {
                                        populateMetadata.AppendLine($@"                PopulateMetadata<{candidate.ReturnType}>(del.Method, builder);");
                                    }
                                }
                            }
                        }
                    }
                }

                populateMetadata.AppendLine($@"                builder.Metadata.Add(new SourceKey(@""{invocation.SyntaxTree.FilePath}"", {lineNumber}));");

                var returnType = methodModel.MethodInfo.ReturnType;

                if (AwaitableInfo.IsTypeAwaitable(returnType, out var awaitableInfo))
                {
                    returnType = awaitableInfo.ResultType;
                }

                AnalyzeResultTypesForIResultMethods(method, returnType);

                if (returnType.Equals(typeof(void)))
                {
                    // Don't add metadata
                }
                else if (wellKnownTypes.IEndpointMetadataProviderType?.IsAssignableFrom(returnType) == true)
                {
                    // TODO: Result<T> internally uses reflection to call this method on it's generic args conditionally
                    // we can avoid that reflection here.

                    // Static abstract call
                    populateMetadata.AppendLine($@"                PopulateMetadata<{returnType}>(del.Method, builder);");
                }
                else if (returnType.Equals(typeof(string)))
                {
                    // Add string plaintext
                    populateMetadata.AppendLine($@"                builder.Metadata.Add(ResponseTypeMetadata.Create(""text/plain""));");
                }
                else if (!wellKnownTypes.IResultType.IsAssignableFrom(returnType))
                {
                    // Add JSON
                    populateMetadata.AppendLine($@"                builder.Metadata.Add(ResponseTypeMetadata.Create(""application/json"", {(returnType.GetTypeSymbol().IsAnonymousType ? "typeof(T)" : $"typeof({returnType.ToString().Replace("?", string.Empty)})")}));");
                }

                var thunkBuilder = method.ReturnType.IsAnonymousType ? genericThunks : thunks;

                // Generate code here for this thunk
                thunkBuilder.Append($@"            [(@""{invocation.SyntaxTree.FilePath}"", {lineNumber})] = (
           (del, builder) => 
            {{
{metadataPreReqs.ToString().TrimEnd()}
{populateMetadata.ToString().TrimEnd()}
            }}, 
           (del, builder) => 
            {{
                var handler = ({fullDelegateType})del;
                EndpointFilterDelegate? filteredInvocation = null;
{preReq}{runtimeChecks.ToString().TrimEnd()}
                if (builder.FilterFactories.Count > 0)
                {{
                    filteredInvocation = BuildFilterDelegate(ic => 
                    {{
                        if (ic.HttpContext.Response.StatusCode == 400)
                        {{
                            return System.Threading.Tasks.ValueTask.FromResult<object?>(Results.Empty);
                        }}
                        {filteredInvocationText}
                    }},
                    builder,
                    handler.Method);
                }}

{codeGenerator}
                return filteredInvocation is null ? RequestHandler : RequestHandlerFiltered;
            }}),

");

                if (!generatedMethodSignatures.Add($"{callName}_{fullDelegateType}"))
                {
                    continue;
                }

                var verbArgument = MethodDescriptions.TryGetValue(callName, out var verb) ? $"{verb}Verb" : "null";

                string methodText = null;

                if (method.ReturnType.IsAnonymousType)
                {
                    methodText = @$"        /// <summary>
        /// Adds a <see cref=""RouteEndpoint""/> to the <see cref=""IEndpointRouteBuilder""/> that matches HTTP{(verb is not null ? " " + verb.ToUpperInvariant() : "")} requests
        /// for the specified pattern.
        /// </summary>
        /// <param name=""endpoints"">The <see cref=""IEndpointRouteBuilder""/> to add the route to.</param>
        /// <param name=""pattern"">The route pattern.</param>
        /// <param name=""handler"">The delegate executed when the endpoint is matched.</param>
        /// <returns>A <see cref=""RouteHandlerBuilder""/> that can be used to further customize the endpoint.</returns>
        internal static Microsoft.AspNetCore.Builder.RouteHandlerBuilder {callName}<T>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, {fullDelegateType} handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = """", [System.Runtime.CompilerServices.CallerLineNumber]int lineNumber = 0)
        {{
            return MapCore<T>(endpoints, pattern, handler, {verbArgument}, filePath, lineNumber);
        }}

";
                }
                else
                {
                    methodText = @$"        /// <summary>
        /// Adds a <see cref=""RouteEndpoint""/> to the <see cref=""IEndpointRouteBuilder""/> that matches HTTP{(verb is not null ? " " + verb.ToUpperInvariant() : "")} requests
        /// for the specified pattern.
        /// </summary>
        /// <param name=""endpoints"">The <see cref=""IEndpointRouteBuilder""/> to add the route to.</param>
        /// <param name=""pattern"">The route pattern.</param>
        /// <param name=""handler"">The delegate executed when the endpoint is matched.</param>
        /// <returns>A <see cref=""RouteHandlerBuilder""/> that can be used to further customize the endpoint.</returns>
        internal static Microsoft.AspNetCore.Builder.RouteHandlerBuilder {callName}(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, {fullDelegateType} handler, [System.Runtime.CompilerServices.CallerFilePath] string filePath = """", [System.Runtime.CompilerServices.CallerLineNumber]int lineNumber = 0)
        {{
            return MapCore(endpoints, pattern, handler, {verbArgument}, filePath, lineNumber);
        }}

";
                }

                sb.Append(methodText);
            }

            string sourceKeyText = wellKnownTypes.SourceKeyType is not null ? "" : @"
namespace Microsoft.AspNetCore.Builder
{
    internal record SourceKey(string Path, int Line);
}
";

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
#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

using MetadataPopulator = System.Action<System.Delegate, Microsoft.AspNetCore.Builder.EndpointBuilder>;
using RequestDelegateFactoryFunc = System.Func<System.Delegate, Microsoft.AspNetCore.Builder.EndpointBuilder, Microsoft.AspNetCore.Http.RequestDelegate>;
{sourceKeyText}
internal static class GeneratedRouteBuilderExtensions
{{
    private static readonly string[] GetVerb = new[] {{ HttpMethods.Get }};
    private static readonly string[] PostVerb = new[] {{ HttpMethods.Post }};
    private static readonly string[] PutVerb = new[]  {{ HttpMethods.Put }};
    private static readonly string[] DeleteVerb = new[] {{ HttpMethods.Delete }};
    private static readonly string[] PatchVerb = new[] {{ HttpMethods.Patch }};

    private class GenericThunks<T>
    {{
        public static readonly System.Collections.Generic.Dictionary<(string, int), (MetadataPopulator, RequestDelegateFactoryFunc)> map = new()
        {{
{genericThunks}
        }};
    }}

    private static readonly System.Collections.Generic.Dictionary<(string, int), (MetadataPopulator, RequestDelegateFactoryFunc)> map = new()
    {{
{thunks}
    }};
{sb.ToString().TrimEnd()}

    private static Microsoft.AspNetCore.Builder.RouteHandlerBuilder MapCore<T>(
        this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, 
        string pattern, 
        System.Delegate handler,
        IEnumerable<string> httpMethods,
        string filePath,
        int lineNumber)
    {{
        var (populate, factory) = GenericThunks<T>.map[(filePath, lineNumber)];

        return GetOrAddRouteEndpointDataSource(routes).AddRouteHandler(RoutePatternFactory.Parse(pattern), handler, httpMethods, isFallback: false, populate, factory);
    }}

    private static Microsoft.AspNetCore.Builder.RouteHandlerBuilder MapCore(
        this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routes, 
        string pattern, 
        System.Delegate handler,
        IEnumerable<string> httpMethods,
        string filePath,
        int lineNumber)
    {{
        var (populate, factory) = map[(filePath, lineNumber)];

        return GetOrAddRouteEndpointDataSource(routes).AddRouteHandler(RoutePatternFactory.Parse(pattern), handler, httpMethods, isFallback: false, populate, factory);
    }}

    private static SourceGeneratedRouteEndpointDataSource GetOrAddRouteEndpointDataSource(IEndpointRouteBuilder endpoints)
    {{
        SourceGeneratedRouteEndpointDataSource? routeEndpointDataSource = null;

        foreach (var dataSource in endpoints.DataSources)
        {{
            if (dataSource is SourceGeneratedRouteEndpointDataSource foundDataSource)
            {{
                routeEndpointDataSource = foundDataSource;
                break;
            }}
        }}

        if (routeEndpointDataSource is null)
        {{
            routeEndpointDataSource = new SourceGeneratedRouteEndpointDataSource(endpoints.ServiceProvider);
            endpoints.DataSources.Add(routeEndpointDataSource);
        }}

        return routeEndpointDataSource;
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

    private static void PopulateMetadata<T>(System.Reflection.ParameterInfo parameter, EndpointBuilder builder) where T : Microsoft.AspNetCore.Http.Metadata.IEndpointParameterMetadataProvider
    {{
        T.PopulateMetadata(parameter, builder);
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
    private static Microsoft.Extensions.Primitives.StringValues? ResolveByQuery(HttpContext context, string key) => context.Request.Query[key];
    private static Microsoft.Extensions.Primitives.StringValues? ResolveByRoute(HttpContext context, string key) => context.Request.RouteValues[key]?.ToString();
    private static Func<HttpContext, ValueTask<T>> ResolveService<T>(bool isOptional) => isOptional
        ? (HttpContext httpContext) => new ValueTask<T>(httpContext.RequestServices.GetService<T>())
        : (HttpContext httpContext) => new ValueTask<T>(httpContext.RequestServices.GetRequiredService<T>());
    private static async ValueTask<T?> ResolveBodyOptional<T>(HttpContext httpContext) => await ResolveBody<T>(httpContext, true);
    private static async ValueTask<T?> ResolveBodyRequired<T>(HttpContext httpContext) => await ResolveBody<T>(httpContext, false);
    private static async ValueTask<T?> ResolveBody<T>(HttpContext httpContext, bool allowEmpty)
    {{
        var feature = httpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpRequestBodyDetectionFeature>();
        if (feature?.CanHaveBody == true)
        {{
            if (!httpContext.Request.HasJsonContentType())
            {{
                httpContext.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                return default;
            }}
            try
            {{
                var bodyValue = await httpContext.Request.ReadFromJsonAsync<T>();
                if (!allowEmpty && bodyValue == null)
                {{
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                }}
                return bodyValue;
            }}
            catch (IOException)
            {{
                return default;
            }}
            catch (System.Text.Json.JsonException)
            {{
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return default;
            }}
        }}
        return default;
    }}

    private sealed class AcceptsTypeMetadata : Microsoft.AspNetCore.Http.Metadata.IAcceptsMetadata
    {{
        public IReadOnlyList<string> ContentTypes {{ get; }}

        public Type RequestType {{ get; }}

        public bool IsOptional {{ get; }}

        public AcceptsTypeMetadata(Type type, bool isOptional, string[] contentTypes)
        {{
            RequestType = type ?? throw new ArgumentNullException(nameof(type));

            if (contentTypes == null)
            {{
                throw new ArgumentNullException(nameof(contentTypes));
            }}

            ContentTypes = contentTypes;
            IsOptional = isOptional;
        }}
    }}

    private sealed class ResponseTypeMetadata : Microsoft.AspNetCore.Http.Metadata.IProducesResponseTypeMetadata
    {{
        public Type Type {{ get; set; }} = typeof(void);

        public int StatusCode {{ get; set; }} = 200;

        public IEnumerable<string> ContentTypes {{ get; init; }} = Enumerable.Empty<string>();

        public static ResponseTypeMetadata Create(string contentType, Type? type = null)
        {{
            return new ResponseTypeMetadata {{ ContentTypes = new[] {{ contentType }}, Type = type }};
        }}

        public static ResponseTypeMetadata Create(int statusCode)
        {{
            return new ResponseTypeMetadata {{ StatusCode = statusCode }};
        }}
    }}

    private sealed class SourceGeneratedRouteEndpointDataSource : EndpointDataSource
    {{
        private readonly List<RouteEntry> _routeEntries = new();
        private readonly IServiceProvider _applicationServices;

        public SourceGeneratedRouteEndpointDataSource(IServiceProvider applicationServices)
        {{
            _applicationServices = applicationServices;
        }}

        public RouteHandlerBuilder AddRouteHandler(
            RoutePattern pattern,
            Delegate routeHandler,
            IEnumerable<string> httpMethods,
            bool isFallback,
            MetadataPopulator metadataPopulator,
            RequestDelegateFactoryFunc requestDelegateFactoryFunc)
        {{
            var conventions = new ThrowOnAddAfterEndpointBuiltConventionCollection();
            var finallyConventions = new ThrowOnAddAfterEndpointBuiltConventionCollection();

            var routeAttributes = RouteAttributes.RouteHandler;
            if (isFallback)
            {{
                routeAttributes |= RouteAttributes.Fallback;
            }}

            _routeEntries.Add(new()
            {{
                RoutePattern = pattern,
                RouteHandler = routeHandler,
                HttpMethods = httpMethods,
                RouteAttributes = routeAttributes,
                Conventions = conventions,
                FinallyConventions = finallyConventions,
                RequestDelegateFactory = requestDelegateFactoryFunc,
                MetadataPopulator = metadataPopulator,
            }});

            return new RouteHandlerBuilder(new[] {{ new ConventionBuilder(conventions, finallyConventions) }});
        }}

        public override IReadOnlyList<RouteEndpoint> Endpoints
        {{
            get
            {{
                var endpoints = new RouteEndpoint[_routeEntries.Count];
                for (int i = 0; i < _routeEntries.Count; i++)
                {{
                    endpoints[i] = (RouteEndpoint)CreateRouteEndpointBuilder(_routeEntries[i]).Build();
                }}
                return endpoints;
            }}
        }}

        public override IReadOnlyList<RouteEndpoint> GetGroupedEndpoints(RouteGroupContext context)
        {{
            var endpoints = new RouteEndpoint[_routeEntries.Count];
            for (int i = 0; i < _routeEntries.Count; i++)
            {{
                endpoints[i] = (RouteEndpoint)CreateRouteEndpointBuilder(_routeEntries[i], context.Prefix, context.Conventions, context.FinallyConventions).Build();
            }}
            return endpoints;
        }}

        public override IChangeToken GetChangeToken() => NullChangeToken.Singleton;

        private RouteEndpointBuilder CreateRouteEndpointBuilder(
            RouteEntry entry, RoutePattern? groupPrefix = null, IReadOnlyList<Action<EndpointBuilder>>? groupConventions = null, IReadOnlyList<Action<EndpointBuilder>>? groupFinallyConventions = null)
        {{
            var pattern = RoutePatternFactory.Combine(groupPrefix, entry.RoutePattern);
            var handler = entry.RouteHandler;
            var isRouteHandler = (entry.RouteAttributes & RouteAttributes.RouteHandler) == RouteAttributes.RouteHandler;
            var isFallback = (entry.RouteAttributes & RouteAttributes.Fallback) == RouteAttributes.Fallback;

            var order = isFallback ? int.MaxValue : 0;
            var displayName = pattern.RawText ?? pattern.ToString();

            if (entry.HttpMethods is not null)
            {{
                // Prepends the HTTP method to the DisplayName produced with pattern + method name
                displayName = $""HTTP: {{string.Join("", "", entry.HttpMethods)}} {{displayName}}"";
            }}

            if (isFallback)
            {{
                displayName = $""Fallback {{displayName}}"";
            }}

            // If we're not a route handler, we started with a fully realized (although unfiltered) RequestDelegate, so we can just redirect to that
            // while running any conventions. We'll put the original back if it remains unfiltered right before building the endpoint.
            RequestDelegate? factoryCreatedRequestDelegate = null;

            // Let existing conventions capture and call into builder.RequestDelegate as long as they do so after it has been created.
            RequestDelegate redirectRequestDelegate = context =>
            {{
                if (factoryCreatedRequestDelegate is null)
                {{
                    throw new InvalidOperationException(""Resources.RouteEndpointDataSource_RequestDelegateCannotBeCalledBeforeBuild"");
                }}

                return factoryCreatedRequestDelegate(context);
            }};

            // Add MethodInfo and HttpMethodMetadata (if any) as first metadata items as they are intrinsic to the route much like
            // the pattern or default display name. This gives visibility to conventions like WithOpenApi() to intrinsic route details
            // (namely the MethodInfo) even when applied early as group conventions.
            RouteEndpointBuilder builder = new(redirectRequestDelegate, pattern, order)
            {{
                DisplayName = displayName,
                ApplicationServices = _applicationServices,
            }};

            if (isRouteHandler)
            {{
                builder.Metadata.Add(handler.Method);
            }}

            if (entry.HttpMethods is not null)
            {{
                builder.Metadata.Add(new HttpMethodMetadata(entry.HttpMethods));
            }}

            // Apply group conventions before entry-specific conventions added to the RouteHandlerBuilder.
            if (groupConventions is not null)
            {{
                foreach (var groupConvention in groupConventions)
                {{
                    groupConvention(builder);
                }}
            }}

            // Any metadata inferred directly inferred by RDF or indirectly inferred via IEndpoint(Parameter)MetadataProviders are
            // considered less specific than method-level attributes and conventions but more specific than group conventions
            // so inferred metadata gets added in between these. If group conventions need to override inferred metadata,
            // they can do so via IEndpointConventionBuilder.Finally like the do to override any other entry-specific metadata.
            if (isRouteHandler)
            {{
                entry.MetadataPopulator(entry.RouteHandler, builder);
            }}

            // Add delegate attributes as metadata before entry-specific conventions but after group conventions.
            var attributes = handler.Method.GetCustomAttributes();
            if (attributes is not null)
            {{
                foreach (var attribute in attributes)
                {{
                    builder.Metadata.Add(attribute);
                }}
            }}

            entry.Conventions.IsReadOnly = true;
            foreach (var entrySpecificConvention in entry.Conventions)
            {{
                entrySpecificConvention(builder);
            }}

            // If no convention has modified builder.RequestDelegate, we can use the RequestDelegate returned by the RequestDelegateFactory directly.
            var conventionOverriddenRequestDelegate = ReferenceEquals(builder.RequestDelegate, redirectRequestDelegate) ? null : builder.RequestDelegate;

            if (isRouteHandler || builder.FilterFactories.Count > 0)
            {{
                factoryCreatedRequestDelegate = entry.RequestDelegateFactory(entry.RouteHandler, builder);
            }}

            Debug.Assert(factoryCreatedRequestDelegate is not null);

            // Use the overridden RequestDelegate if it exists. If the overridden RequestDelegate is merely wrapping the final RequestDelegate,
            // it will still work because of the redirectRequestDelegate.
            builder.RequestDelegate = conventionOverriddenRequestDelegate ?? factoryCreatedRequestDelegate;

            entry.FinallyConventions.IsReadOnly = true;
            foreach (var entryFinallyConvention in entry.FinallyConventions)
            {{
                entryFinallyConvention(builder);
            }}

            if (groupFinallyConventions is not null)
            {{
                // Group conventions are ordered by the RouteGroupBuilder before
                // being provided here.
                foreach (var groupFinallyConvention in groupFinallyConventions)
                {{
                    groupFinallyConvention(builder);
                }}
            }}

            return builder;
        }}
        private struct RouteEntry
        {{
            public MetadataPopulator MetadataPopulator {{ get; init; }}
            public RequestDelegateFactoryFunc RequestDelegateFactory {{ get; init; }}
            public RoutePattern RoutePattern {{ get; init; }}
            public Delegate RouteHandler {{ get; init; }}
            public IEnumerable<string> HttpMethods {{ get; init; }}
            public RouteAttributes RouteAttributes {{ get; init; }}
            public ThrowOnAddAfterEndpointBuiltConventionCollection Conventions {{ get; init; }}
            public ThrowOnAddAfterEndpointBuiltConventionCollection FinallyConventions {{ get; init; }}
        }}

        [Flags]
        private enum RouteAttributes
        {{
            // The endpoint was defined by a RequestDelegate, RequestDelegateFactory.Create() should be skipped unless there are endpoint filters.
            None = 0,
            // This was added as Delegate route handler, so RequestDelegateFactory.Create() should always be called.
            RouteHandler = 1,
            // This was added by MapFallback.
            Fallback = 2,
        }}

        // This private class is only exposed to internal code via ICollection<Action<EndpointBuilder>> in RouteEndpointBuilder where only Add is called.
        private sealed class ThrowOnAddAfterEndpointBuiltConventionCollection : List<Action<EndpointBuilder>>, ICollection<Action<EndpointBuilder>>
        {{
            // We throw if someone tries to add conventions to the RouteEntry after endpoints have already been resolved meaning the conventions
            // will not be observed given RouteEndpointDataSource is not meant to be dynamic and uses NullChangeToken.Singleton.
            public bool IsReadOnly {{ get; set; }}

            void ICollection<Action<EndpointBuilder>>.Add(Action<EndpointBuilder> convention)
            {{
                if (IsReadOnly)
                {{
                    throw new InvalidOperationException(""Resources.RouteEndpointDataSource_ConventionsCannotBeModifiedAfterBuild"");
                }}

                Add(convention);
            }}
        }}

        private class ConventionBuilder : IEndpointConventionBuilder
        {{
            private readonly ICollection<Action<EndpointBuilder>> _conventions;
            private readonly ICollection<Action<EndpointBuilder>> _finallyConventions;
            public ConventionBuilder(ICollection<Action<EndpointBuilder>> conventions, ICollection<Action<EndpointBuilder>> finallyConventions)
            {{
                _conventions = conventions;
                _finallyConventions = finallyConventions;
            }}

            /// <summary>
            /// Adds the specified convention to the builder. Conventions are used to customize <see cref=""EndpointBuilder""/> instances.
            /// </summary>
            /// <param name=""convention"">The convention to add to the builder.</param>
            public void Add(Action<EndpointBuilder> convention)
            {{
                _conventions.Add(convention);
            }}
            public void Finally(Action<EndpointBuilder> finalConvention)
            {{
                _finallyConventions.Add(finalConvention);
            }}
        }}
    }}
}}
#endif
";
            if (sb.Length > 0)
            {
                context.AddSource($"RouteBuilderExtensions.g.cs", SourceText.From(mapActionsText, Encoding.UTF8));
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

            public List<InvocationExpressionSyntax> MapActions { get; } = new();

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
                    MapActions.Add(mapActionCall);
                }
            }
        }
    }

    class Diagnostics
    {
        public static readonly DiagnosticDescriptor UnknownDelegateType = new DiagnosticDescriptor("MIN001", "DelegateTypeUnknown", "Unable to determine the parameter and return types from expression \"{0}\", only method groups, lambda expressions or readonly fields/variables are allowed", "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnableToResolveParameter = new DiagnosticDescriptor("MIN002", "ParameterSourceUnknown", "Unable to resolve \"{0}\", consider adding [FromXX] attributes to disambiguate the parameter source", "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnableToResolveTryParseForType = new DiagnosticDescriptor("MIN003", "MissingTryParseForType", "Unable to find a static {0}.TryParse(string, out {0}) implementation", "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MultipleParametersConsumingBody = new DiagnosticDescriptor("MIN005", "MultipleParametersFromBody", "Detecting multiple parameters that attempt to read from the body, consider adding [FromXX] attributes to disambiguate the parameter source", "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor AnonymousTypesAsParametersAreNotSupported = new DiagnosticDescriptor("MIN006", "AnonymousTypesAsParametersAreNotSupported", "Anonymous types are not supported as parameters", "Usage", DiagnosticSeverity.Error, isEnabledByDefault: true);
    }
}
