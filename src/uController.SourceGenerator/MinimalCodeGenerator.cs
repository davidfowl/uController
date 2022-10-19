﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

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

        public HashSet<Type> FromBodyTypes { get; set; } = new HashSet<Type>();

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
            foreach (var parameter in method.Parameters)
            {
                var parameterName = "arg_" + parameter.Name.Replace("_", "__");
                EmitParameter(ref hasAwait, ref hasFromBody, ref hasFromForm, parameter, parameterName);
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

        private void EmitParameter(ref bool hasAwait, ref bool hasFromBody, ref bool hasFromForm, ParameterModel parameter, string parameterName)
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
            // TODO: PipeReader
            //else if (parameter.ParameterType.Equals(typeof(PipeReader)))
            //{
            //    WriteLine($"var {parameterName} = httpContext.Request.BodyReader;");
            //}
            else if (parameter.FromRoute != null)
            {
                GenerateConvert(parameterName, parameter.ParameterType, parameter.FromRoute, "httpContext.Request.RouteValues", nullable: true);
            }
            else if (parameter.FromQuery != null)
            {
                GenerateConvert(parameterName, parameter.ParameterType, parameter.FromQuery, "httpContext.Request.Query");
            }
            else if (parameter.FromHeader != null)
            {
                GenerateConvert(parameterName, parameter.ParameterType, parameter.FromHeader, "httpContext.Request.Headers");
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
                GenerateConvert(parameterName, parameter.ParameterType, parameter.FromForm, "formCollection");
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
                    FromBodyTypes.Add(parameter.ParameterType);
                }
                else
                {
                    // TODO: Handle empty body (required parameters);
                    WriteLine($"var {parameterName} = await httpContext.Request.ReadFromJsonAsync<{S(parameter.ParameterType)}>();");
                    FromBodyTypes.Add(parameter.ParameterType);
                }

                hasAwait = true;
            }
            else
            {
                // TODO: Look for bind async
                // Error if we can't determine the binding source for this parameter
                parameter.Unresovled = true;
                WriteLine($"{S(parameter.ParameterType)} {parameterName} = default;");
            }
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
            foreach (var parameter in method.Parameters)
            {
                var parameterName = "arg_" + parameter.Name.Replace("_", "__");
                EmitParameter(ref hasAwait, ref hasFromBody, ref hasFromForm, parameter, parameterName);
            }

            Write("var result = await ");

            WriteNoIndent($"filteredInvocation(new DefaultEndpointFilterInvocationContext(httpContext");
            bool first = false;
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

        private void GenerateConvert(string sourceName, Type type, string key, string sourceExpression, bool nullable = false)
        {
            // TODO: Handle arrays
            if (type.Equals(typeof(string)))
            {
                WriteLine($"var {sourceName} = {sourceExpression}[\"{key}\"]" + (nullable ? "?.ToString();" : ".ToString();"));
            }
            else
            {
                WriteLine($"var {sourceName}_Value = {sourceExpression}[\"{key}\"]" + (nullable ? "?.ToString();" : ".ToString();"));
                WriteLine($"{S(type)} {sourceName};");

                // TODO: Handle cases where TryParse isn't available
                // type = Unwrap(type) ?? type;
                var unwrappedType = Unwrap(type);
                if (unwrappedType == null)
                {
                    // Type isn't nullable
                    WriteLine($"if ({sourceName}_Value == null || !{S(type)}.TryParse({sourceName}_Value, out {sourceName}))");
                    WriteLine("{");
                    Indent();
                    WriteLine($"{sourceName} = default;");
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
