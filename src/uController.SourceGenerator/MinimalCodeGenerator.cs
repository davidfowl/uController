using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Primitives;

namespace uController.CodeGeneration
{
    class MinimalCodeGenerator
    {
        private readonly StringBuilder _codeBuilder = new();
        private readonly MetadataLoadContext _metadataLoadContext;
        private int _indent;

        public MinimalCodeGenerator(MetadataLoadContext metadataLoadContext)
        {
            _metadataLoadContext = metadataLoadContext;
        }

        public HashSet<ParameterModel> FromBodyTypes { get; set; } = new HashSet<ParameterModel>();

        // Pretty print the type name
        private string S(Type type) => type.ToString();

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
                Write("");
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
                var parameterName = "arg_" + parameter.Name.Replace("_", "__");
                if (!first)
                {
                    WriteNoIndent(", ");
                }
                WriteNoIndent(parameterName);
                first = false;
            }
            WriteLineNoIndent(");");

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
            if (_metadataLoadContext.Resolve<IResult>().IsAssignableFrom(unwrappedType))
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
            var methodStartIndex = _codeBuilder.Length + 4 * _indent;
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

        private void EmitParameter(ref bool hasAwait, ref bool hasFromBody, ref bool hasFromForm, ref bool generatedParamCheck, ParameterModel parameter, string parameterName)
        {
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
            else if (parameter.ParameterType.Equals(typeof(IFormCollection)))
            {
                WriteLine($"var {parameterName} = await httpContext.Request.ReadFormAsync();");
                hasAwait = true;
            }
            else if (parameter.ParameterType.Equals(_metadataLoadContext.LoadFromAssemblyName("System.Security.Claims").GetType("System.Security.Claims.ClaimsPrincipal")))
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
            // TODO: PipeReader
            //else if (parameter.ParameterType.Equals(typeof(PipeReader)))
            //{
            //    WriteLine($"var {parameterName} = httpContext.Request.BodyReader;");
            //}
            else if (parameter.FromRoute != null)
            {
                if (!GenerateConvert(parameterName, parameter.ParameterType, parameter.FromRoute, "httpContext.Request.RouteValues", ref generatedParamCheck, nullable: true))
                {
                    parameter.Unresovled = true;
                }
            }
            else if (parameter.FromQuery != null)
            {
                if (!GenerateConvert(parameterName, parameter.ParameterType, parameter.FromQuery, "httpContext.Request.Query", ref generatedParamCheck))
                {
                    parameter.Unresovled = true;
                }
            }
            else if (parameter.FromHeader != null)
            {
                if (!GenerateConvert(parameterName, parameter.ParameterType, parameter.FromHeader, "httpContext.Request.Headers", ref generatedParamCheck))
                {
                    parameter.Unresovled = true;
                }
            }
            else if (parameter.FromServices)
            {
                WriteLine($"var {parameterName} = httpContext.RequestServices.GetRequiredService<{S(parameter.ParameterType)}>();");
            }
            else if (parameter.FromForm != null)
            {
                if (!hasFromForm)
                {
                    WriteLine($"var formCollection = await httpContext.Request.ReadFormAsync();");
                    hasAwait = true;
                    hasFromForm = true;
                }

                if (!GenerateConvert(parameterName, parameter.ParameterType, parameter.FromForm, "formCollection", ref generatedParamCheck))
                {
                    parameter.Unresovled = true;
                }
            }
            else if (parameter.FromBody)
            {
                if (!hasFromBody)
                {
                    hasFromBody = true;
                }

                // TODO: PipeReader
                if (parameter.ParameterType.Equals(typeof(Stream)))
                {
                    WriteLine($"var {parameterName} = httpContext.Request.Body;");
                    FromBodyTypes.Add(parameter);
                }
                else
                {
                    // TODO: Handle empty body (required parameters)

                    WriteLine($"var {parameterName} = await httpContext.Request.ReadFromJsonAsync<{S(parameter.ParameterType)}>();");
                    FromBodyTypes.Add(parameter);
                }

                hasAwait = true;
            }
            else
            {
                // Error if we can't determine the binding source for this parameter
                var parameterType = Unwrap(parameter.ParameterType) ?? parameter.ParameterType;

                if (HasBindAsync(parameterType, out var bindAsyncMethod, out var parameterCount))
                {
                    if (parameterCount == 1)
                    {
                        WriteLine($"var {parameterName} = await {S(bindAsyncMethod.DeclaringType)}.BindAsync(httpContext);");
                    }
                    else
                    {
                        parameter.RequiresParameterInfo = true;

                        WriteLine($"var {parameterName} = await {S(bindAsyncMethod.DeclaringType)}.BindAsync(httpContext, parameterInfos[{parameter.Index}]);");
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
                    if (!GenerateConvert(parameterName, parameter.ParameterType, parameter.Name, $"{parameter.GeneratedName}RouteOrQueryResolver", ref generatedParamCheck, methodCall: true, tryParseMethod: tryParseMethod))
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

        private bool HasTryParseMethod(Type t, out MethodInfo mi)
        {
            mi = GetStaticMethodFromHierarchy(t, "TryParse", new[] { typeof(string), t.MakeByRefType() }, m => m.ReturnType.Equals(typeof(bool)));

            // TODO: Add IFormatProvider overload

            return mi != null;
        }

        private bool HasBindAsync(Type t, out MethodInfo mi, out int parameterCount)
        {
            var httpContextType = _metadataLoadContext.Resolve<HttpContext>();

            // TODO: Validate return type

            mi = GetStaticMethodFromHierarchy(t, "BindAsync", new[] { httpContextType }, m => true);

            mi ??= GetStaticMethodFromHierarchy(t, "BindAsync", new[] { httpContextType, _metadataLoadContext.Resolve<ParameterInfo>() }, m => true);

            parameterCount = mi?.GetParameters().Length ?? 0;

            return mi != null;
        }

        private bool GenerateConvert(string sourceName, Type type, string key, string sourceExpression, ref bool generatedParamCheck, bool nullable = false, bool methodCall = false, MethodInfo tryParseMethod = null)
        {
            var getter = methodCall ? $@"{sourceExpression}(httpContext, ""{key}"")" : $@"{sourceExpression}[""{key}""]";

            // TODO: Handle specific types (Uri, DateTime etc) with relevant options
            if (type.Equals(typeof(string)))
            {
                WriteLine($"var {sourceName} = {getter}" + (nullable ? "?.ToString();" : ".ToString();"));
            }
            else if (type.Equals(typeof(StringValues)))
            {
                WriteLine($"var {sourceName} = {getter};");
            }
            else if (type.Equals(typeof(string[])))
            {
                WriteLine($"var {sourceName} = {getter}.ToArray();");
            }
            else
            {
                var unwrappedType = Unwrap(type) ?? type;

                if (tryParseMethod is null)
                {
                    if (!HasTryParseMethod(unwrappedType, out tryParseMethod))
                    {
                        WriteLine($"{S(type)} {sourceName} = default;");
                        return false;
                    }
                }

                if (type.IsArray)
                {
                    var elementType = type.GetElementType();
                    WriteLine($"var {sourceName}_Value = {getter}.ToArray();");
                    WriteLine($"{elementType}[] {sourceName} = default;");
                    WriteLine($"for (var i = 0; i < {sourceName}.Length; i++)");
                    WriteLine("{");
                    Indent();
                    WriteLine($"{sourceName} ??= new {elementType}[{sourceName}_Value.Length];");
                    GenerateTryParse(tryParseMethod, $"{sourceName}_Value[i]", $"{sourceName}[i]", elementType, ref generatedParamCheck);
                    Unindent();
                    WriteLine("}");

                    // TODO: Nullability
                    WriteLine($"{sourceName} ??= System.Array.Empty<{elementType}>();");
                }
                else
                {
                    WriteLine($"var {sourceName}_Value = {getter}" + (nullable ? "?.ToString();" : ".ToString();"));
                    WriteLine($"{S(type)} {sourceName};");

                    GenerateTryParse(tryParseMethod, $"{sourceName}_Value", sourceName, type, ref generatedParamCheck);
                }
            }
            return true;
        }

        private void GenerateTryParse(MethodInfo tryParseMethod, string sourceName, string outputName, Type type, ref bool generatedParamCheck)
        {
            // TODO: Support different TryParse overloads

            var unwrappedType = Unwrap(type) ?? type;

            if (Unwrap(type) == null)
            {
                generatedParamCheck = true;
                // Type isn't nullable
                WriteLine($"if ({sourceName} == null || !{S(type)}.TryParse({sourceName}, out {outputName}))");
                WriteLine("{");
                Indent();
                WriteLine($"{outputName} = default;");
                WriteLine("wasParamCheckFailure = true;");
                Unindent();
                WriteLine("}");
            }
            else
            {
                WriteLine($"if ({sourceName} != null && {S(unwrappedType)}.TryParse({sourceName}, out var {outputName}_Temp))");
                WriteLine("{");
                Indent();
                WriteLine($"{outputName} = {outputName}_Temp;");
                Unindent();
                WriteLine("}");
                WriteLine("else");
                WriteLine("{");
                Indent();
                WriteLine($"{outputName} = default;");
                Unindent();
                WriteLine("}");
            }
        }

        private MethodInfo GetStaticMethodFromHierarchy(Type type, string name, Type[] parameterTypes, Func<MethodInfo, bool> validateReturnType)
        {
            bool IsMatch(MethodInfo method) => method is not null && !method.IsAbstract && validateReturnType(method);

            MethodInfo Search(Type t) => t.GetMethods().FirstOrDefault(m => m.Name == name && m.IsPublic && m.IsStatic && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes));

            var methodInfo = Search(type);

            if (IsMatch(methodInfo))
            {
                return methodInfo;
            }

            var candidateInterfaceMethodInfo = default(MethodInfo);

            // Check all interfaces for implementations. Fail if there are duplicates.
            foreach (var implementedInterface in type.GetInterfaces())
            {
                var interfaceMethod = Search(implementedInterface);

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
