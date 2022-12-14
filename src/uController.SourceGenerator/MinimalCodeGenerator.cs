using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Primitives;
using Roslyn.Reflection;

namespace uController.SourceGenerator
{
    class MinimalCodeGenerator
    {
        private readonly StringBuilder _codeBuilder = new();
        private readonly WellKnownTypes _wellKnownTypes;
        private int _indent;

        public MinimalCodeGenerator(WellKnownTypes wellKnownTypes)
        {
            _wellKnownTypes = wellKnownTypes;
        }

        public HashSet<ParameterModel> BodyParameters { get; set; } = new HashSet<ParameterModel>();

        private Type Unwrap(Type type)
        {
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                // instantiated generic type only
                Type genericType = type.GetGenericTypeDefinition();
                if (genericType.Equals(typeof(Nullable<>)))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            return null;
        }

        internal static Type UnwrapValueTask(Type type)
        {
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                // instantiated generic type only
                var genericType = type.GetGenericTypeDefinition();
                if (genericType.Equals(typeof(ValueTask<>)))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            return null;
        }

        public void Indent()
        {
            _indent++;
        }

        public void Unindent()
        {
            _indent--;
        }

        public void Generate(MethodModel method)
        {
            GenerateMethod(method);
            WriteLine("");
            GenerateFilteredMethod(method);
        }

        private void GenerateMethod(MethodModel method)
        {
            var methodStartIndex = _codeBuilder.Length + 4 * _indent;
            WriteLine($"async {typeof(Task)} {method.UniqueName}({typeof(HttpContext)} httpContext)");
            WriteLine("{");
            Indent();

            // Declare locals
            var hasAwait = false;
            var hasFromBody = false;
            var hasFromForm = false;
            var generatedParamCheck = false;

            var paramFailureStartIndex = _codeBuilder.Length + 4 * _indent;
            var paramCheckExpression = "var wasParamCheckFailure = false;";

            WriteLine(paramCheckExpression);

            foreach (var parameter in method.Parameters)
            {
                var parameterName = parameter.GeneratedName;
                EmitParameter(ref hasAwait, ref hasFromBody, ref hasFromForm, ref generatedParamCheck, parameter, parameterName);
            }

            var resultExpression = "";

            AwaitableInfo awaitableInfo = default;
            if (method.MethodInfo.ReturnType.Equals(typeof(void)))
            {
                resultExpression = "";
            }
            else
            {
                if (AwaitableInfo.IsTypeAwaitable(method.MethodInfo.ReturnType, out awaitableInfo))
                {
                    if (awaitableInfo.ResultType.Equals(typeof(void)))
                    {
                        if (hasAwait)
                        {
                            resultExpression = "await ";
                        }
                        else
                        {
                            resultExpression = "return ";
                        }
                    }
                    else
                    {
                        resultExpression = "var result = await ";
                        hasAwait = true;
                    }
                }
                else
                {
                    resultExpression = "var result = ";
                }
            }

            if (generatedParamCheck)
            {
                WriteLine("if (wasParamCheckFailure)");
                WriteLine("{");
                Indent();
                WriteLine("httpContext.Response.StatusCode = 400;");
                if (hasAwait)
                {
                    WriteLine("return;");
                }
                else
                {
                    WriteLine("return Task.CompletedTask;");
                }

                Unindent();
                WriteLine("}");
            }
            else
            {
                var currentIndent = 4 * _indent;
                _codeBuilder.Remove(paramFailureStartIndex - currentIndent - Environment.NewLine.Length, paramCheckExpression.Length + currentIndent + Environment.NewLine.Length);
            }

            Write(resultExpression);

            WriteNoIndent($"handler(");
            bool first = true;
            foreach (var parameter in method.Parameters)
            {
                var parameterName = parameter.GeneratedName;
                if (!first)
                {
                    WriteNoIndent(", ");
                }
                WriteNoIndent(parameterName);
                first = false;
            }

            if (!hasAwait && method.MethodInfo.ReturnType.Equals(typeof(ValueTask)))
            {
                // Convert the ValueTask to a Task
                WriteLineNoIndent(").AsTask();");
            }
            else
            {
                WriteLineNoIndent(");");
            }

            if (!hasAwait)
            {
                // Remove " async " from method signature.
                _codeBuilder.Remove(methodStartIndex, 6);
            }

            void AwaitOrReturn(string executeAsync)
            {
                if (hasAwait)
                {
                    Write("await ");
                }
                else
                {
                    Write("return ");
                }

                WriteLineNoIndent(executeAsync);
            }

            var unwrappedType = awaitableInfo.ResultType ?? method.MethodInfo.ReturnType;
            if (_wellKnownTypes.IResultType.IsAssignableFrom(unwrappedType))
            {
                AwaitOrReturn("result.ExecuteAsync(httpContext);");
            }
            else if (unwrappedType.Equals(typeof(string)))
            {
                AwaitOrReturn($"httpContext.Response.WriteAsync(result);");
            }
            else if (unwrappedType.Equals(typeof(object)))
            {
                AwaitOrReturn("ExecuteObjectResult(result, httpContext);");
            }
            else if (!unwrappedType.Equals(typeof(void)))
            {
                AwaitOrReturn($"httpContext.Response.WriteAsJsonAsync(result);");
            }
            else if (!hasAwait && method.MethodInfo.ReturnType.Equals(typeof(void)))
            {
                // If awaitableInfo.ResultType is void, we've already returned the awaitable directly.
                WriteLine($"return {typeof(Task)}.CompletedTask;");
            }

            Unindent();
            WriteLine("}");
        }

        private void GenerateFilteredMethod(MethodModel method)
        {
            WriteLine($"async {typeof(Task)} {method.UniqueName}Filtered({typeof(HttpContext)} httpContext)");
            WriteLine("{");
            Indent();

            // Declare locals
            var hasAwait = false;
            var hasFromBody = false;
            var hasFromForm = false;
            var generatedParamCheck = false;

            var paramFailureStartIndex = _codeBuilder.Length + 4 * _indent;
            var paramCheckExpression = "var wasParamCheckFailure = false;";

            WriteLine(paramCheckExpression);

            foreach (var parameter in method.Parameters)
            {
                var parameterName = parameter.GeneratedName;
                EmitParameter(ref hasAwait, ref hasFromBody, ref hasFromForm, ref generatedParamCheck, parameter, parameterName);
            }

            if (generatedParamCheck)
            {
                WriteLine("if (wasParamCheckFailure)");
                WriteLine("{");
                Indent();
                WriteLine("httpContext.Response.StatusCode = 400;");
                Unindent();
                WriteLine("}");
            }
            else
            {
                var currentIndent = 4 * _indent;
                _codeBuilder.Remove(paramFailureStartIndex - currentIndent - Environment.NewLine.Length, paramCheckExpression.Length + currentIndent + Environment.NewLine.Length);
            }

            Write("var result = await ");

            WriteNoIndent($"filteredInvocation(new DefaultEndpointFilterInvocationContext(httpContext");
            foreach (var parameter in method.Parameters)
            {
                WriteNoIndent(", ");
                WriteNoIndent(parameter.GeneratedName);
            }
            WriteLineNoIndent("));");

            void AwaitOrReturn(string executeAsync)
            {
                Write("await ");

                WriteLineNoIndent(executeAsync);
            }

            AwaitOrReturn("ExecuteObjectResult(result, httpContext);");

            Unindent();
            WriteLine("}");
        }

        private void EmitParameter(ref bool hasAwait, ref bool hasFromBody, ref bool hasForm, ref bool generatedParamCheck, ParameterModel parameter, string parameterName)
        {
            var defaultValue = parameter.ParameterSymbol.HasExplicitDefaultValue
                ? parameter.ParameterSymbol.ExplicitDefaultValue
                : null;
            var isOptional = parameter.ParameterSymbol.IsOptional ||
                             parameter.ParameterSymbol.NullableAnnotation == NullableAnnotation.Annotated;
            if (parameter.ParameterType.Equals(typeof(HttpContext)))
            {
                WriteLine($"var {parameterName} = httpContext;");
            }
            else if (parameter.ParameterType.Equals(typeof(HttpRequest)))
            {
                WriteLine($"var {parameterName} = httpContext.Request;");
            }
            else if (parameter.ParameterType.Equals(typeof(HttpResponse)))
            {
                WriteLine($"var {parameterName} = httpContext.Response;");
            }
            else if (parameter.ParameterType.Equals(typeof(IFormFile)))
            {
                if (!hasForm)
                {
                    WriteLine($"var formCollection = await httpContext.Request.ReadFormAsync();");
                    hasAwait = true;
                    hasForm = true;
                }

                WriteLine($@"var {parameterName} = formCollection.Files[""{parameter.Name}""];");
                parameter.ReadFromForm = true;
            }
            else if (parameter.ParameterType.Equals(typeof(IFormCollection)))
            {
                if (!hasForm)
                {
                    WriteLine($"var formCollection = await httpContext.Request.ReadFormAsync();");
                    hasAwait = true;
                    hasForm = true;
                }

                WriteLine($"var {parameterName} = formCollection;");
                parameter.ReadFromForm = true;
            }
            else if (parameter.ParameterType.Equals(typeof(ClaimsPrincipal)))
            {
                WriteLine($"var {parameterName} = httpContext.User;");
            }
            else if (parameter.ParameterType.Equals(typeof(CancellationToken)))
            {
                WriteLine($"var {parameterName} = httpContext.RequestAborted;");
            }
            else if (parameter.ParameterType.Equals(typeof(Stream)))
            {
                WriteLine($"var {parameterName} = httpContext.Request.Body;");
            }
            else if (parameter.ParameterType.Equals(typeof(PipeReader)))
            {
                WriteLine($"var {parameterName} = httpContext.Request.BodyReader;");
            }
            else if (parameter.FromRoute != null)
            {
                if (!GenerateConvert(parameterName, parameter.ParameterInfo, parameter.ParameterType, defaultValue, parameter.FromRoute, "httpContext.Request.RouteValues", ref generatedParamCheck, nullable: true))
                {
                    parameter.Unresovled = true;
                }
            }
            else if (parameter.FromQuery != null)
            {
                if (!GenerateConvert(parameterName, parameter.ParameterInfo, parameter.ParameterType, defaultValue, parameter.FromQuery, "httpContext.Request.Query", ref generatedParamCheck, sourcedFromStringValue: true))
                {
                    parameter.Unresovled = true;
                }
            }
            else if (parameter.FromHeader != null)
            {
                if (!GenerateConvert(parameterName, parameter.ParameterInfo, parameter.ParameterType, defaultValue, parameter.FromHeader, "httpContext.Request.Headers", ref generatedParamCheck, sourcedFromStringValue: true))
                {
                    parameter.Unresovled = true;
                }
            }
            else if (parameter.FromServices)
            {
                if (isOptional)
                {
                    WriteLine($"var {parameterName} = httpContext.RequestServices.GetService<{parameter.ParameterType}>();");
                }
                else
                {
                    WriteLine($"var {parameterName} = httpContext.RequestServices.GetRequiredService<{parameter.ParameterType}>();");
                }
            }
            else if (parameter.FromForm != null)
            {
                if (!hasForm)
                {
                    WriteLine($"var formCollection = await httpContext.Request.ReadFormAsync();");
                    hasAwait = true;
                    hasForm = true;
                }

                if (!GenerateConvert(parameterName, parameter.ParameterInfo, parameter.ParameterType, defaultValue, parameter.FromForm, "formCollection", ref generatedParamCheck))
                {
                    parameter.Unresovled = true;
                }

                parameter.ReadFromForm = true;
            }
            else if (parameter.FromBody)
            {
                BodyParameters.Add(parameter);
                var allowEmptyKey = "AllowEmpty";
                // Check existence of argument beforehand to avoid null-ref
                if (parameter.FromBodyAttributeData is {} fromBodyAttribute &&
                    fromBodyAttribute.NamedArguments.Any(n => n.MemberName == allowEmptyKey))
                {
                    var allowEmpty = fromBodyAttribute?.GetNamedArgument<bool>(allowEmptyKey) ?? false;
                    isOptional |= allowEmpty;
                }

                if (parameter.ParameterType.Equals(typeof(PipeReader)))
                {
                    WriteLine($"var {parameterName} = httpContext.Request.BodyReader;");
                }
                else if (parameter.ParameterType.Equals(typeof(Stream)))
                {
                    WriteLine($"var {parameterName} = httpContext.Request.Body;");
                }
                else
                {
                    WriteLine($"var {parameterName} = await ResolveBody<{parameter.ParameterType}>(httpContext, {(isOptional ? "true" : "false")});");
                }

                hasAwait = true;
            }
            else
            {
                // Error if we can't determine the binding source for this parameter
                var parameterType = Unwrap(parameter.ParameterType) ?? parameter.ParameterType;

                if (HasBindAsync(parameterType, out var bindAsyncMethod, out var parameterCount, out var returnType))
                {
                    if (parameterCount == 1)
                    {
                        WriteLine($"var {parameterName}Nullable = await {bindAsyncMethod.DeclaringType}.BindAsync(httpContext);");
                    }
                    else
                    {
                        parameter.RequiresParameterInfo = true;

                        WriteLine($"var {parameterName}Nullable = await {bindAsyncMethod.DeclaringType}.BindAsync(httpContext, {parameter.GeneratedName}ParameterInfo);");
                    }

                    var generatedBindAsyncAssignment = false;

                    // Emit null check when the parameter is required
                    // but the `BindAsync` can resolve to a nullable value
                    if (UnwrapValueTask(returnType) is RoslynType innerReturn // Gets the T in ValueTask<T>
                        && parameter.ParameterSymbol.NullableAnnotation != NullableAnnotation.Annotated
                        && innerReturn.TypeSymbol.NullableAnnotation == NullableAnnotation.Annotated) // Gets the T in T?
                    {
                        WriteLine($"if ({parameterName}Nullable == null)");
                        WriteLine("{");
                        Indent();
                        WriteLine("httpContext.Response.StatusCode = 400;");
                        WriteLine("return;");
                        Unindent();
                        WriteLine("}");

                        // If the result of `BindAsync` is nullable and the parameter
                        // is not a value type then emit the value of the resolved
                        // nullable value
                        if (innerReturn.TypeSymbol.IsValueType)
                        {
                            WriteLine($"var {parameterName} = {parameterName}Nullable.Value;");
                            generatedBindAsyncAssignment = true;
                        }
                    }

                    // Get the default out of the parameter
                    if (!generatedBindAsyncAssignment)
                    {
                        WriteLine($"var {parameterName} = {parameterName}Nullable;");
                    }


                    hasAwait = true;
                }

                else if (HasTryParseMethod(parameterType, out var tryParseMethod) ||
                         parameterType.Equals(typeof(string)) ||
                         parameterType.Equals(typeof(StringValues)) ||
                         parameterType.Equals(typeof(string[])) ||
                         parameterType.IsArray &&
                         HasTryParseMethod(parameterType.GetElementType(), out tryParseMethod))
                {
                    parameter.QueryOrRoute = true;

                    // Fallback to resolver
                    if (!GenerateConvert(parameterName, parameter.ParameterInfo, parameter.ParameterType, defaultValue, parameter.Name, $"{parameter.GeneratedName}RouteOrQueryResolver", ref generatedParamCheck, methodCall: true, tryParseMethod: tryParseMethod, sourcedFromStringValue: true))
                    {
                        parameter.Unresovled = true;
                    }
                }
                else
                {
                    parameter.BodyOrService = true;
                    hasAwait = true;

                    WriteLine($"var {parameterName} = await {parameterName}ServiceOrBodyResolver(httpContext);");
                }
            }
        }

        private bool HasTryParseMethod(Type type, out MethodInfo mi)
        {
            // TODO: Handle specific types (Uri, DateTime etc) with relevant options

            mi = null;

            if (type.IsEnum)
            {
                // Use Enum.TryParse<T>(string, bool, out T) for enums
                mi = GetEnumTryParseMethod();
            }

            mi ??= GetStaticMethodFromHierarchy(type, "TryParse", new[] { typeof(string), type.MakeByRefType() }, m => m.ReturnType.Equals(typeof(bool)));

            mi ??= GetStaticMethodFromHierarchy(type, "TryParse", new[] { typeof(string), _wellKnownTypes.IFormatProviderType, type.MakeByRefType() }, m => m.ReturnType.Equals(typeof(bool)));

            return mi != null;
        }

        private bool HasBindAsync(Type type, out MethodInfo mi, out int parameterCount, out Type returnType)
        {
            mi = GetStaticMethodFromHierarchy(type, "BindAsync", new[] { _wellKnownTypes.HttpContextType }, m => true);

            mi ??= GetStaticMethodFromHierarchy(type, "BindAsync", new[] { _wellKnownTypes.HttpContextType, _wellKnownTypes.ParamterInfoType }, m => true);

            parameterCount = mi?.GetParameters().Length ?? 0;

            returnType = mi?.ReturnType;

            return mi != null;
        }

        private bool GenerateConvert(string sourceName, ParameterInfo parameterInfo, Type type, object defaultValue, string key, string sourceExpression, ref bool generatedParamCheck, bool nullable = false, bool methodCall = false, MethodInfo tryParseMethod = null, bool sourcedFromStringValue = false)
        {
            var getter = methodCall
                ? $@"{sourceExpression}(httpContext, ""{key}"")"
                : $@"{sourceExpression}[""{key}""]";

            WriteLine($"var getter{sourceName}Value = {getter};");

            if (!(parameterInfo as RoslynParameterInfo).IsOptional)
            {
                generatedParamCheck = true;
                if (sourcedFromStringValue)
                {
                    WriteLine($"if (string.IsNullOrEmpty(getter{sourceName}Value))");
                }
                else
                {
                    WriteLine($"if (getter{sourceName}Value == null)");
                }

                WriteLine("{");
                Indent();
                WriteLine("wasParamCheckFailure = true;");
                Unindent();
                WriteLine("}");
            }

            if (type.Equals(typeof(string)))
            {
                if (defaultValue is null)
                {
                    WriteLine($"var {sourceName} = getter{sourceName}Value" + (nullable ? "?.ToString();" : ".ToString();"));
                }
                else
                {
                    if (nullable)
                    {
                        WriteLine($@"var {sourceName} = getter{sourceName}Value?.ToString() ?? ""{defaultValue}"";");
                    }
                    else
                    {
                        WriteLine($@"var {sourceName}Str = getter{sourceName}Value.ToString();");
                        WriteLine($@"var {sourceName} = string.IsNullOrEmpty({sourceName}Str) ? ""{defaultValue}"" : {sourceName}Str;");
                    }
                }
            }
            else if (type.Equals(typeof(StringValues)))
            {
                WriteLine($"var {sourceName} = getter{sourceName}Value.Value;");
            }
            else if (type.Equals(typeof(StringValues?)))
            {
                WriteLine($"var {sourceName} = getter{sourceName}Value;");
            }
            else if (type.Equals(typeof(string[])))
            {
                WriteLine($"var {sourceName} = getter{sourceName}Value?.ToArray();");
            }
            else
            {
                var unwrappedType = Unwrap(type) ?? type;

                if (tryParseMethod is null)
                {
                    if (!HasTryParseMethod(unwrappedType, out tryParseMethod))
                    {
                        WriteLine($"{type} {sourceName} = default;");
                        return false;
                    }
                }

                if (type.IsArray)
                {
                    var elementType = type.GetElementType();
                    WriteLine($"var {sourceName}_Value = getter{sourceName}Value?.ToArray();");
                    WriteLine($"{elementType}[] {sourceName} = default;");
                    WriteLine($"for (var i = 0; i < {sourceName}.Length; i++)");
                    WriteLine("{");
                    Indent();
                    WriteLine($"{sourceName} ??= new {elementType}[{sourceName}_Value.Length];");
                    GenerateTryParse(tryParseMethod, $"{sourceName}_Value[i]", $"{sourceName}[i]", elementType, null,
                                     ref generatedParamCheck);
                    Unindent();
                    WriteLine("}");

                    // TODO: Nullability
                    WriteLine($"{sourceName} ??= System.Array.Empty<{elementType}>();");
                }
                else
                {
                    WriteLine($"var {sourceName}_Value = getter{sourceName}Value" + (nullable ? "?.ToString();" : ".ToString();"));
                    WriteLine($"{type} {sourceName};");

                    GenerateTryParse(tryParseMethod, $"{sourceName}_Value", sourceName, type, defaultValue,
                                     ref generatedParamCheck);
                }
            }

            return true;
        }

        private void GenerateTryParse(MethodInfo tryParseMethod, string sourceName, string outputName, Type type, object defaultValue, ref bool generatedParamCheck)
        {
            var underlyingType = Unwrap(type);

            // Support different TryParse overloads
            string TryParseExpression(string outputExpression) => (tryParseMethod.DeclaringType, tryParseMethod.GetParameters().Length) switch
            {
                (_, 2) => $"{tryParseMethod.DeclaringType}.TryParse({sourceName}, out {outputExpression})",
                (var type, 3) when type.Equals(_wellKnownTypes.EnumType) => $"{tryParseMethod.DeclaringType}.TryParse({sourceName}, true, out {outputExpression})",
                (_, 3) => $"{tryParseMethod.DeclaringType}.TryParse({sourceName}, System.Globalization.CultureInfo.InvariantCulture, out {outputExpression})",
                _ => throw new NotSupportedException("Unknown TryParse method")
            };

            // Type isn't nullable
            if (underlyingType is null)
            {
                generatedParamCheck = true;

                // No source, no default value
                // No source but has a default value
                // Unable to parse
                if (defaultValue is null)
                {
                    WriteLine($"if ({sourceName} == null || !{TryParseExpression(outputName)})");
                    WriteLine("{");
                    Indent();
                    WriteLine($"{outputName} = default;");
                    WriteLine("wasParamCheckFailure = true;");
                    Unindent();
                    WriteLine("}");
                }
                else
                {
                    WriteLine($"if (string.IsNullOrEmpty({sourceName}))");
                    WriteLine("{");
                    Indent();
                    WriteLine($"{outputName} = {defaultValue};");
                    Unindent();
                    WriteLine("}");
                    WriteLine($"else if (!{TryParseExpression(outputName)})");
                    WriteLine("{");
                    Indent();
                    WriteLine($"{outputName} = default;");
                    WriteLine("wasParamCheckFailure = true;");
                    Unindent();
                    WriteLine("}");
                }
            }
            else
            {
                WriteLine($"if ({sourceName} != null && {TryParseExpression($"var {outputName}_Temp")})");
                WriteLine("{");
                Indent();
                WriteLine($"{outputName} = {outputName}_Temp;");
                Unindent();
                WriteLine("}");
                WriteLine("else");
                WriteLine("{");
                Indent();
                if (defaultValue is null)
                {
                    WriteLine($"{outputName} = default;");
                }
                else
                {
                    WriteLine($"{outputName} = {defaultValue};");
                }
                Unindent();
                WriteLine("}");
            }
        }

        private MethodInfo GetEnumTryParseMethod()
        {
            var tryParse = (from m in _wellKnownTypes.EnumType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                            let parameters = m.GetParameters()
                            where parameters.Length == 3 && m.Name == "TryParse" && m.IsGenericMethod &&
                                  parameters[0].ParameterType.Equals(typeof(string)) &&
                                  parameters[1].ParameterType.Equals(typeof(bool))
                            select m).FirstOrDefault();
            return tryParse;
        }

        private MethodInfo GetStaticMethodFromHierarchy(Type type, string name, Type[] parameterTypes, Func<MethodInfo, bool> validateReturnType)
        {
            bool IsMatch(MethodInfo method) => method is not null && !method.IsAbstract && validateReturnType(method);

            var methodInfo = type.GetMethod(name,
                                            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy,
                                            binder: null,
                                            types: parameterTypes,
                                            modifiers: default);

            if (IsMatch(methodInfo))
            {
                return methodInfo;
            }

            var candidateInterfaceMethodInfo = default(MethodInfo);

            // Check all interfaces for implementations. Fail if there are duplicates.
            foreach (var implementedInterface in type.GetInterfaces())
            {
                var interfaceMethod = implementedInterface.GetMethod(name,
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: parameterTypes,
                    modifiers: default);

                if (IsMatch(interfaceMethod))
                {
                    if (candidateInterfaceMethodInfo is not null)
                    {
                        return null;
                    }

                    candidateInterfaceMethodInfo = interfaceMethod;
                }
            }

            return candidateInterfaceMethodInfo;
        }

        private void WriteLineNoIndent(string value)
        {
            _codeBuilder.AppendLine(value);
        }

        private void WriteNoIndent(string value)
        {
            _codeBuilder.Append(value);
        }

        private void Write(string value)
        {
            if (_indent > 0)
            {
                _codeBuilder.Append(new string(' ', _indent * 4));
            }
            _codeBuilder.Append(value);
        }

        private void WriteLine(string value)
        {
            if (_indent > 0)
            {
                _codeBuilder.Append(new string(' ', _indent * 4));
            }
            _codeBuilder.AppendLine(value);
        }

        public override string ToString()
        {
            return _codeBuilder.ToString();
        }
    }
}
