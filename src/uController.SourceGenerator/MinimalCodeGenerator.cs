using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Primitives;

namespace uController.CodeGeneration
{
    public class MinimalCodeGenerator
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
        private string S(Type type) => TypeNameHelper.GetTypeDisplayName(type);

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
                var parameterName = "arg_" + parameter.Name.Replace("_", "__");
                EmitParameter(ref hasAwait, ref hasFromBody, ref hasFromForm, ref generatedParamCheck, parameter, parameterName);
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

            AwaitableInfo awaitableInfo = default;
            // Populate locals
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
                            Write("await ");
                        }
                        else
                        {
                            Write("return ");
                        }
                    }
                    else
                    {
                        Write("var result = await ");
                        hasAwait = true;
                    }
                }
                else
                {
                    Write("var result = ");
                }
            }

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
                WriteLine("if (result is IResult r)");
                WriteLine("{");
                Indent();
                AwaitOrReturn("r.ExecuteAsync(httpContext);");
                Unindent();
                WriteLine("}");

                WriteLine("else if (result is string s)");
                WriteLine("{");
                Indent();
                AwaitOrReturn($"httpContext.Response.WriteAsync(s);");
                Unindent();
                WriteLine("}");

                WriteLine("else");
                WriteLine("{");
                Indent();
                AwaitOrReturn($"httpContext.Response.WriteAsJsonAsync(result);");
                Unindent();
                WriteLine("}");
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
                var parameterName = "arg_" + parameter.Name.Replace("_", "__");
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
                var parameterName = "arg_" + parameter.Name.Replace("_", "__");

                WriteNoIndent(", ");
                WriteNoIndent(parameterName);
            }
            WriteLineNoIndent("));");

            void AwaitOrReturn(string executeAsync)
            {
                Write("await ");

                WriteLineNoIndent(executeAsync);
            }

            WriteLine("if (result is IResult r)");
            WriteLine("{");
            Indent();
            AwaitOrReturn("r.ExecuteAsync(httpContext);");
            Unindent();
            WriteLine("}");

            WriteLine("else if (result is string s)");
            WriteLine("{");
            Indent();
            AwaitOrReturn($"httpContext.Response.WriteAsync(s);");
            Unindent();
            WriteLine("}");

            WriteLine("else");
            WriteLine("{");
            Indent();
            AwaitOrReturn($"httpContext.Response.WriteAsJsonAsync(result);");
            Unindent();
            WriteLine("}");

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
                // TODO: Error handling when there are multiple
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
                    // TODO: Handle empty body (required parameters);
                    WriteLine($"var {parameterName} = await httpContext.Request.ReadFromJsonAsync<{S(parameter.ParameterType)}>();");
                    FromBodyTypes.Add(parameter);
                }

                hasAwait = true;
            }
            else
            {
                // Error if we can't determine the binding source for this parameter
                var parameterType = Unwrap(parameter.ParameterType) ?? parameter.ParameterType;

                // There should only be one BindAsync method with these parameters since C# does not allow overloading on return type.
                var methodInfo = GetStaticMethodFromHierarchy(parameterType, "BindAsync", new[] { _metadataLoadContext.Resolve<HttpContext>() }, m => true);

                if (methodInfo is not null)
                {
                    WriteLine($"var {parameterName} = await {S(methodInfo.DeclaringType)}.BindAsync(httpContext);");
                    hasAwait = true;

                    // TODO: Look for more bind async variants
                }
                else
                {
                    // Look for more try parse overloads too
                    methodInfo = GetStaticMethodFromHierarchy(parameterType, "TryParse", new[] { typeof(string), parameterType.MakeByRefType() }, m => m.ReturnType.Equals(typeof(bool)));

                    if (methodInfo is not null || parameterType.Equals(typeof(string)) || parameterType.Equals(typeof(StringValues)))
                    {
                        // Fallback to query string
                        if (!GenerateConvert(parameterName, parameter.ParameterType, parameter.Name, "httpContext.Request.Query", ref generatedParamCheck))
                        {
                            parameter.Unresovled = true;
                        }
                    }
                    else if (!parameter.Method.DisableInferBodyFromParameters)
                    {
                        // Assume body here
                        WriteLine($"var {parameterName} = await httpContext.Request.ReadFromJsonAsync<{S(parameter.ParameterType)}>();");
                        FromBodyTypes.Add(parameter);
                        hasAwait = true;
                    }
                    else
                    {
                        parameter.Unresovled = true;
                        WriteLine($"{S(parameter.ParameterType)} {parameterName} = default;");
                    }
                }
            }
        }

        private bool GenerateConvert(string sourceName, Type type, string key, string sourceExpression, ref bool generatedParamCheck, bool nullable = false)
        {
            // TODO: Handle specific types (Uri, DateTime etc) with relevant options
            // TODO: Handle arrays
            if (type.Equals(typeof(string)))
            {
                WriteLine($"var {sourceName} = {sourceExpression}[\"{key}\"]" + (nullable ? "?.ToString();" : ".ToString();"));
            }
            else if (type.Equals(typeof(StringValues)))
            {
                WriteLine($"var {sourceName} = {sourceExpression}[\"{key}\"];");
            }
            else
            {
                var unwrappedType = Unwrap(type) ?? type;

                // TODO: Support more overloads
                var methodInfo = GetStaticMethodFromHierarchy(unwrappedType, "TryParse", new[] { typeof(string), unwrappedType.MakeByRefType() }, m => m.ReturnType.Equals(typeof(bool)));

                if (methodInfo is null)
                {
                    WriteLine($"{S(type)} {sourceName} = default;");
                    return false;
                }

                WriteLine($"var {sourceName}_Value = {sourceExpression}[\"{key}\"]" + (nullable ? "?.ToString();" : ".ToString();"));
                WriteLine($"{S(type)} {sourceName};");

                if (unwrappedType == null)
                {
                    generatedParamCheck = true;
                    // Type isn't nullable
                    WriteLine($"if ({sourceName}_Value == null || !{S(type)}.TryParse({sourceName}_Value, out {sourceName}))");
                    WriteLine("{");
                    Indent();
                    WriteLine($"{sourceName} = default;");
                    WriteLine("wasParamCheckFailure = true;");
                    Unindent();
                    WriteLine("}");
                }
                else
                {
                    WriteLine($"if ({sourceName}_Value != null && {S(unwrappedType)}.TryParse({sourceName}_Value, out var {sourceName}_Temp))");
                    WriteLine("{");
                    Indent();
                    WriteLine($"{sourceName} = {sourceName}_Temp;");
                    Unindent();
                    WriteLine("}");
                    WriteLine("else");
                    WriteLine("{");
                    Indent();
                    WriteLine($"{sourceName} = default;");
                    Unindent();
                    WriteLine("}");

                }
            }
            return true;
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
