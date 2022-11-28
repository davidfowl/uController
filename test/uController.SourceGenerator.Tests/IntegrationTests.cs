using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MinimalApis.Extensions.Binding;
using Moq;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace uController.SourceGenerator.Tests;

// TODO: Migrate all tests from https://github.com/dotnet/aspnetcore/blob/main/src/Http/Http.Extensions/test/RequestDelegateFactoryTests.cs
// This is a subset.

public class IntegrationTests
{
    [Fact]
    public async Task MapGet_NoParameters_StringReturn()
    {
        // Arrange
        var source = @"
app.MapGet(""/"", () => ""Hello world!"");
";

        // Act
        var (results, compilation) = await RunGenerator(source);

        // Assert
        Assert.Empty(results.Diagnostics);

        var builderFunc = CreateInvocationFromCompilation(compilation);
        var builder = CreateEndpointBuilder();
        _ = builderFunc(builder);

        var dataSource = Assert.Single(builder.DataSources);
        var endpoint = Assert.Single(dataSource.Endpoints);

        var sourceKeyMetadata = endpoint.Metadata.GetMetadata<SourceKey>();
        Assert.NotNull(sourceKeyMetadata);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("GET", method);

        await AssertEndpointBehavior(endpoint, "Hello world!", 200);
    }

    [Fact]
    public async Task MapGet_StringRouteParameters_StringReturn()
    {
        // Arrange
        var source = @"
app.MapGet(""/hello/{name}"", (string name) => $""Hello {name}!"");
";

        // Act
        var (results, compilation) = await RunGenerator(source);

        // Assert
        Assert.Empty(results.Diagnostics);

        var builderFunc = CreateInvocationFromCompilation(compilation);
        var builder = CreateEndpointBuilder();
        _ = builderFunc(builder);

        var dataSource = Assert.Single(builder.DataSources);
        var endpoint = Assert.Single(dataSource.Endpoints);

        var sourceKeyMetadata = endpoint.Metadata.GetMetadata<SourceKey>();
        Assert.NotNull(sourceKeyMetadata);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("GET", method);

        await AssertEndpointBehavior(
            endpoint,
            "Hello Tester!",
            200,
            routeValues: new(new[] { new KeyValuePair<string, string?>("name", "Tester") }));
    }

    [Fact]
    public async Task MapGet_StringQueryParameters_StringReturn()
    {
        // Arrange
        var source = @"
app.MapGet(""/hello"", (string name) => $""Hello {name}!"");
";

        // Act
        var (results, compilation) = await RunGenerator(source);

        // Assert
        Assert.Empty(results.Diagnostics);

        var builderFunc = CreateInvocationFromCompilation(compilation);
        var builder = CreateEndpointBuilder();
        _ = builderFunc(builder);

        var dataSource = Assert.Single(builder.DataSources);
        var endpoint = Assert.Single(dataSource.Endpoints);

        var sourceKeyMetadata = endpoint.Metadata.GetMetadata<SourceKey>();
        Assert.NotNull(sourceKeyMetadata);

        var methodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        Assert.NotNull(methodMetadata);
        var method = Assert.Single(methodMetadata!.HttpMethods);
        Assert.Equal("GET", method);

        await AssertEndpointBehavior(
            endpoint,
            "Hello David!",
            200,
            query: QueryString.Create("name", "David"));
    }

    [Fact]
    public async Task MapGet_ImplicitFromService()
    {
        // Arrange
        var source = $@"
app.MapGet(""/"", ({typeof(TodoService)} todo) => todo.ToString());
";

        // Act
        var (results, compilation) = await RunGenerator(source);

        // Assert
        Assert.Empty(results.Diagnostics);

        var builderFunc = CreateInvocationFromCompilation(compilation);
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new ServiceCollection().AddSingleton<TodoService>().BuildServiceProvider()));
        _ = builderFunc(builder);

        var dataSource = Assert.Single(builder.DataSources);
        var endpoint = Assert.Single(dataSource.Endpoints);

        var sourceKeyMetadata = endpoint.Metadata.GetMetadata<SourceKey>();
        Assert.NotNull(sourceKeyMetadata);

        await AssertEndpointBehavior(endpoint, typeof(TodoService).ToString(), 200, builder.ServiceProvider);
    }

    [Fact]
    public async Task MapGetWithNamedFromRouteParameter_UsesFromRouteName()
    {
        // Arrange
        var source = $@"
app.MapGet(""/{{value}}"", ([FromRoute(Name = ""value"")] int id, HttpContext httpContext) =>
{{
    httpContext.Items[""value""] = id;
}});
";
        // Act
        var (results, compilation) = await RunGenerator(source);

        // Assert
        Assert.Empty(results.Diagnostics);

        var builderFunc = CreateInvocationFromCompilation(compilation);
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(new EmptyServiceProvider()));
        _ = builderFunc(builder);

        var dataSource = Assert.Single(builder.DataSources);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        // Assert that we don't fallback to the query string
        var httpContext = new DefaultHttpContext();

        httpContext.Request.RouteValues["value"] = "42";

        await endpoint.RequestDelegate!(httpContext);

        Assert.Equal(42, httpContext.Items["value"]);
    }

    public static IEnumerable<object[]> NoResult
    {
        get
        {
            var testAction = """
            void TestAction(HttpContext httpContext)
            {
                httpContext.Items.Add("invoked", true);
            }
            app.MapGet("/", TestAction); 
            """;

            // This binds to the RequestDelegate overload
            //var taskTestAction = """
            //Task TaskTestAction(HttpContext httpContext)
            //{
            //    httpContext.Items.Add("invoked", true);
            //    return Task.CompletedTask;
            //}
            //app.MapGet("/", TaskTestAction);
            //""";

            var valueTaskTestAction = """
            ValueTask ValueTaskTestAction(HttpContext httpContext)
            {
                httpContext.Items.Add("invoked", true);
                return ValueTask.CompletedTask;
            }
            app.MapGet("/", ValueTaskTestAction);
            """;

            var staticTestAction = """
            void StaticTestAction(HttpContext httpContext)
            {
                httpContext.Items.Add("invoked", true);
            }
            app.MapGet("/", StaticTestAction);
            """;

            // This binds to the RequestDelegate overload
            //var staticTaskTestAction = """
            //Task StaticTaskTestAction(HttpContext httpContext)
            //{
            //    httpContext.Items.Add("invoked", true);
            //    return Task.CompletedTask;
            //}
            //app.MapGet("/", StaticTaskTestAction);
            //""";

            var staticValueTaskTestAction = """
            ValueTask StaticValueTaskTestAction(HttpContext httpContext)
            {
                httpContext.Items.Add("invoked", true);
                return ValueTask.CompletedTask;
            }
            app.MapGet("/", StaticValueTaskTestAction);
            """;

            return new List<object[]>
                {
                    new object[] { testAction },
                    // new object[] { taskTestAction },
                    new object[] { valueTaskTestAction },
                    new object[] { staticTestAction },
                    // new object[] { staticTaskTestAction },
                    new object[] { staticValueTaskTestAction },
                };
        }
    }

    [Theory]
    [MemberData(nameof(NoResult))]
    public async Task RequestDelegateInvokesAction(string source)
    {
        var requestDelegate = await GetRequestDelegate(source);

        var httpContext = new DefaultHttpContext();

        await requestDelegate(httpContext);

        Assert.True(httpContext.Items["invoked"] as bool?);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromRouteParameterBasedOnParameterName()
    {
        const string paramName = "value";
        const int originalRouteParam = 42;

        var requestDelegate = await GetRequestDelegate(
        """
        static void TestAction(HttpContext httpContext, [FromRoute] int value)
        {
            httpContext.Items.Add("input", value);
        }
        app.MapGet("/", TestAction);
        """);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues[paramName] = originalRouteParam.ToString(NumberFormatInfo.InvariantInfo);

        await requestDelegate(httpContext);

        Assert.Equal(originalRouteParam, httpContext.Items["input"]);
    }

    [Fact]
    public async Task SpecifiedRouteParametersDoNotFallbackToQueryString()
    {
        var requestDelegate = await GetRequestDelegate(
        """
        app.MapGet("/{id}", (int? id, HttpContext httpContext) =>
        {
            if (id is not null)
            {
                httpContext.Items["input"] = id;
            }
        });
        """);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["id"] = "42"
        });

        await requestDelegate(httpContext);

        Assert.Null(httpContext.Items["input"]);
    }

    [Fact]
    public async Task SpecifiedQueryParametersDoNotFallbackToRouteValues()
    {
        var requestDelegate = await GetRequestDelegate(
        """
        app.MapGet("/", (int? id, HttpContext httpContext) =>
        {
            if (id is not null)
            {
                httpContext.Items["input"] = id;
            }
        });
        """);

        var httpContext = new DefaultHttpContext();

        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["id"] = "41"
        });
        httpContext.Request.RouteValues = new()
        {
            ["id"] = "42"
        };

        await requestDelegate(httpContext);

        Assert.Equal(41, httpContext.Items["input"]);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromRouteOptionalParameter()
    {
        var requestDelegate = await GetRequestDelegate(
        """
        static void TestOptional(HttpContext httpContext, [FromRoute] int value = 42)
        {
            httpContext.Items.Add("input", value);
        }
        app.MapGet("/", TestOptional);
        """);

        var httpContext = new DefaultHttpContext();

        await requestDelegate(httpContext);

        Assert.Equal(42, httpContext.Items["input"]);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromNullableOptionalParameter()
    {
        var requestDelegate = await GetRequestDelegate(
        """
        static void TestOptional(HttpContext httpContext, [FromRoute] int? value = 42)
        {
            httpContext.Items.Add("input", value);
        }
        app.MapGet("/", TestOptional);
        """);

        var httpContext = new DefaultHttpContext();

        await requestDelegate(httpContext);

        Assert.Equal(42, httpContext.Items["input"]);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromOptionalStringParameter()
    {
        var requestDelegate = await GetRequestDelegate(
        """
        static void TestOptionalString(HttpContext httpContext, string value = "default")
        {
            httpContext.Items.Add("input", value);
        }
        app.MapGet("/", TestOptionalString);
        """);
        var httpContext = new DefaultHttpContext();

        await requestDelegate(httpContext);

        Assert.Equal("default", httpContext.Items["input"]);
    }

    [Fact]
    public async Task Returns400IfNoMatchingRouteValueForRequiredParam()
    {
        const string unmatchedName = "value";
        const int unmatchedRouteParam = 42;

        var requestDelegate = await GetRequestDelegate(
        """
        void TestAction([FromRoute] int foo, HttpContext httpContext)
        {
            httpContext.Items.Add("deserializedRouteParam", foo);
        }
        app.MapGet("/", TestAction);
        """);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues[unmatchedName] = unmatchedRouteParam.ToString(NumberFormatInfo.InvariantInfo);

        await requestDelegate(httpContext);

        Assert.Equal(400, httpContext.Response.StatusCode);
        Assert.Null(httpContext.Items["deserializedRouteParam"]);
    }

    [Fact]
    public async Task RequestDelegatePrefersBindAsyncOverTryParse()
    {
        var requestDelegate = await GetRequestDelegate(
        $$"""
        app.MapGet("/", (HttpContext httpContext, {{typeof(MyBindAsyncRecord)}} myBindAsyncRecord) =>
        {
            httpContext.Items["myBindAsyncRecord"] = myBindAsyncRecord;
        });
        """);

        var httpContext = new DefaultHttpContext();

        httpContext.Request.Headers.Referer = "https://example.org";

        await requestDelegate(httpContext);

        Assert.Equal(new MyBindAsyncRecord(new Uri("https://example.org")), httpContext.Items["myBindAsyncRecord"]);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromHeaderParameterBasedOnParameterName()
    {
        const string customHeaderName = "X-Custom-Header";
        const int originalHeaderParam = 42;

        var requestDelegate = await GetRequestDelegate(
        $$"""
        void TestAction(HttpContext httpContext, [FromHeader(Name = "{{customHeaderName}}")] int value)
        {
            httpContext.Items["deserializedRouteParam"] = value;
        }
        app.MapGet("/", TestAction);
        """);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[customHeaderName] = originalHeaderParam.ToString(NumberFormatInfo.InvariantInfo);

        await requestDelegate(httpContext);

        Assert.Equal(originalHeaderParam, httpContext.Items["deserializedRouteParam"]);
    }

    public static object[][] ImplicitFromBodyActions(bool withAcceptsMetadata = false)
    {
        var testImpliedFromBody =
        $$""" 
        void TestImpliedFromBody(HttpContext httpContext, {{typeof(Todo)}} todo)
        {
            httpContext.Items.Add("body", todo);
        }
        app.MapPost("/", TestImpliedFromBody);
        """;

        var testImpliedFromBodyInterface =
        $$"""
        void TestImpliedFromBodyInterface(HttpContext httpContext, {{typeof(ITodo)}} todo)
        {
            httpContext.Items.Add("body", todo);
        }
        app.MapPost("/", TestImpliedFromBodyInterface);
        """;

        var testImpliedFromBodyStruct =
        $$"""
        void TestImpliedFromBodyStruct(HttpContext httpContext, {{typeof(TodoStruct)}} todo)
        {
            httpContext.Items.Add("body", todo);
        }
        app.MapPost("/", TestImpliedFromBodyStruct);
        """;

        //void TestImpliedFromBodyStruct_ParameterList([AsParameters] ParametersListWithImplictFromBody args)
        //{
        //    args.HttpContext.Items.Add("body", args.Todo);
        //}

        if (withAcceptsMetadata)
        {
            return new[]
            {
                new object[] { testImpliedFromBody, typeof(Todo), new[] { "application/json" } },
                new object[] { testImpliedFromBodyInterface, typeof(ITodo), new[] { "application/json" } },
                new object[] { testImpliedFromBodyStruct, typeof(TodoStruct), new[] { "application/json" } }
                // new object[] { (Action<ParametersListWithImplictFromBody>)TestImpliedFromBodyStruct_ParameterList },
            };
        }
        return new[]
        {
            new object[] { testImpliedFromBody },
            new object[] { testImpliedFromBodyInterface  },
            new object[] { testImpliedFromBodyStruct  }
            // new object[] { (Action<ParametersListWithImplictFromBody>)TestImpliedFromBodyStruct_ParameterList },
        };
    }

    public static object[][] ExplicitFromBodyActions(bool withAcceptsMetadata = false)
    {
        var TestExplicitFromBody =
        $$"""
        void TestExplicitFromBody(HttpContext httpContext, [FromBody] {{typeof(Todo)}} todo)
        {
            httpContext.Items.Add("body", todo);
        }
        app.MapPost("/", TestExplicitFromBody);
        """;
        // TBD
        //void TestExplicitFromBody_ParameterList([AsParameters] ParametersListWithExplictFromBody args)
        //{
        //    args.HttpContext.Items.Add("body", args.Todo);
        //}

        if (withAcceptsMetadata)
        {

            return new[]
            {
                new object[] { TestExplicitFromBody, typeof(Todo), new[] { "application/json" } },
                // new object[] { (Action<ParametersListWithExplictFromBody>)TestExplicitFromBody_ParameterList },
            };
        }
        return new[]
        {
            new object[] { TestExplicitFromBody  },
            // new object[] { (Action<ParametersListWithExplictFromBody>)TestExplicitFromBody_ParameterList },
        };
    }

    public static object[][] FromBodyActions
    {
        get
        {
            return ExplicitFromBodyActions().Concat(ImplicitFromBodyActions()).ToArray();
        }
    }

    [Theory]
    [MemberData(nameof(FromBodyActions))]
    public async Task RequestDelegatePopulatesFromBodyParameter(string source)
    {
        RequestDelegate requestDelegate = await GetRequestDelegate(source);

        Todo originalTodo = new()
        {
            Name = "Write more tests!"
        };

        var httpContext = new DefaultHttpContext();

        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(originalTodo);
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;

        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var jsonOptions = new JsonOptions();
        jsonOptions.SerializerOptions.Converters.Add(new TodoJsonConverter());

        var mock = new Mock<IServiceProvider>();
        mock.Setup(m => m.GetService(It.IsAny<Type>())).Returns<Type>(t =>
        {
            if (t == typeof(IOptions<JsonOptions>))
            {
                return Options.Create(jsonOptions);
            }
            return null;
        });
        httpContext.RequestServices = mock.Object;

        await requestDelegate(httpContext);

        var deserializedRequestBody = httpContext.Items["body"];
        Assert.NotNull(deserializedRequestBody);
        Assert.Equal(originalTodo.Name, ((ITodo)deserializedRequestBody!).Name);
    }

    public static object?[][] TryParsableParameters
    {
        get
        {
            var now = DateTime.Now;

            var types = new List<(Type, object, object)>
            {
                (typeof(string)         , "plain string", "plain string" ),
                (typeof(int)            , "-42", -42 ),
                (typeof(int?)           , "42", 42),
                (typeof(uint)           , "42", 42U ),
                (typeof(bool)           , "true", true ),
                (typeof(short)          , "-42", (short)-42 ),
                (typeof(ushort)         , "42", (ushort)42 ),
                (typeof(long)           , "-42", -42L ),
                (typeof(ulong)          , "42", 42UL ),
                (typeof(IntPtr)         , "-42", new IntPtr(-42) ),
                (typeof(char)           , "A", 'A' ),
                (typeof(double)         , "0.5", 0.5 ),
                (typeof(float)          , "0.5", 0.5f ),
                (typeof(Half)           , "0.5", (Half)0.5f ),
                (typeof(decimal)        , "0.5", 0.5m ),
                // TBD
                // (typeof(Uri)            , "https://example.org", new Uri("https://example.org") ),
                // (typeof(DateTime)       , now.ToString("o"), now.ToUniversalTime() ),
                (typeof(DateTimeOffset) , "1970-01-01T00:00:00.0000000+00:00", DateTimeOffset.UnixEpoch ),
                (typeof(TimeSpan)       , "00:00:42", TimeSpan.FromSeconds(42) ),
                (typeof(Guid)           , "00000000-0000-0000-0000-000000000000", Guid.Empty ),
                (typeof(Version)        , "6.0.0.42", new Version("6.0.0.42") ),
                (typeof(BigInteger)     , "-42", new BigInteger(-42) ),
                (typeof(IPAddress)      , "127.0.0.1", IPAddress.Loopback ),
                (typeof(IPEndPoint)     , "127.0.0.1:80", new IPEndPoint(IPAddress.Loopback, 80) ),
                (typeof(AddressFamily)  , "Unix", AddressFamily.Unix ),
            };

            // TBD
            //new object[] { (Action<HttpContext, ILOpCode>)Store, "Nop", ILOpCode.Nop },
            //new object[] { (Action<HttpContext, AssemblyFlags>)Store, "PublicKey,Retargetable", AssemblyFlags.PublicKey | AssemblyFlags.Retargetable },
            //new object[] { (Action<HttpContext, int?>)Store, "42", 42 },
            //new object[] { (Action<HttpContext, MyEnum>)Store, "ValueB", MyEnum.ValueB },
            //new object[] { (Action<HttpContext, MyTryParseRecord>)Store, "https://example.org", new MyTryParseRecord(new Uri("https://example.org")) },
            //new object?[] { (Action<HttpContext, int?>)Store, null, null },
            static Type? Unwrap(Type type)
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

            var results = new List<object[]>();
            foreach (var (type, val, expected) in types)
            {
                var source =
                $$"""
                static void Store(HttpContext httpContext, {{(Unwrap(type) is Type t ? $"{t}?" : $"{type}")}} tryParsable)
                {
                    httpContext.Items["tryParsable"] = tryParsable;
                }
                app.MapGet("/{tryParsable}", Store);
                """;

                results.Add(new[] { source, val, expected });
            }

            return results.ToArray();
        }
    }

    [Theory]
    [MemberData(nameof(TryParsableParameters))]
    public async Task RequestDelegatePopulatesUnattributedTryParsableParametersFromRouteValue(string source, string? routeValue, object? expectedParameterValue)
    {
        var requestDelegate = await GetRequestDelegate(source);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues["tryParsable"] = routeValue;

        await requestDelegate(httpContext);

        Assert.Equal(expectedParameterValue, httpContext.Items["tryParsable"]);
    }


    public static object[][] ExplicitFromServiceActions
    {
        get
        {
            var testExplicitFromService =
            $$"""
            void TestExplicitFromService(HttpContext httpContext, [{{typeof(FromServiceAttribute)}}] {{typeof(MyService)}} myService)
            {
                httpContext.Items.Add("service", myService);
            }
            app.MapGet("/", TestExplicitFromService);
            """;

            // TBD
            //void TestExplicitFromService_FromParameterList([AsParameters] ParametersListWithExplictFromService args)
            //{
            //    args.HttpContext.Items.Add("service", args.MyService);
            //}

            var testExplicitFromIEnumerableService =
            $$"""
            void TestExplicitFromIEnumerableService(HttpContext httpContext, [{{typeof(FromServiceAttribute)}}] IEnumerable<{{typeof(MyService)}}> myServices)
            {
                httpContext.Items.Add("service", myServices.Single());
            }
            app.MapGet("/", TestExplicitFromIEnumerableService);
            """;

            var testExplictMultipleFromService =
            $$"""
            void TestExplicitMultipleFromService(HttpContext httpContext, [{{typeof(FromServiceAttribute)}}] {{typeof(MyService)}} myService, [{{typeof(FromServiceAttribute)}}] IEnumerable<{{typeof(MyService)}}> myServices)
            {
                httpContext.Items.Add("service", myService);
            }
            app.MapGet("/", TestExplicitMultipleFromService);
            """;

            return new object[][]
            {
                    new[] { testExplicitFromService },
                    // TBD
                    // new object[] { (Action<ParametersListWithExplictFromService>)TestExplicitFromService_FromParameterList },
                    new[] { testExplicitFromIEnumerableService },
                    new[] { testExplictMultipleFromService },
            };
        }
    }

    public static object[][] ImplicitFromServiceActions
    {
        get
        {
            var testImpliedFromService =
            $$"""
            void TestImpliedFromService(HttpContext httpContext, {{typeof(IMyService)}} myService)
            {
                httpContext.Items.Add("service", myService);
            }
            app.MapGet("/", TestImpliedFromService);
            """;

            //void TestImpliedFromService_FromParameterList([AsParameters] ParametersListWithImplictFromService args)
            //{
            //    args.HttpContext.Items.Add("service", args.MyService);
            //}

            var testImpliedIEnumerableFromService =
            $$"""
            void TestImpliedIEnumerableFromService(HttpContext httpContext, IEnumerable<{{typeof(MyService)}}> myServices)
            {
                httpContext.Items.Add("service", myServices.Single());
            }
            app.MapGet("/", TestImpliedIEnumerableFromService);
            """;

            var testImpliedFromServiceBasedOnContainer =
            $$"""
            void TestImpliedFromServiceBasedOnContainer(HttpContext httpContext, {{typeof(MyService)}} myService)
            {
                httpContext.Items.Add("service", myService);
            }
            app.MapGet("/", TestImpliedFromServiceBasedOnContainer);
            """;

            return new object[][]
            {
                    new[] { testImpliedFromService },
                    // new object[] { (Action<ParametersListWithImplictFromService>)TestImpliedFromService_FromParameterList },
                    new[] { testImpliedIEnumerableFromService },
                    new[] { testImpliedFromServiceBasedOnContainer },
            };
        }
    }


    [Theory]
    [MemberData(nameof(FromServiceActions))]
    public async Task RequestDelegatePopulatesParametersFromServiceWithAndWithoutAttribute(string source)
    {
        var myOriginalService = new MyService();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(myOriginalService);
        serviceCollection.AddSingleton<IMyService>(myOriginalService);
        var services = serviceCollection.BuildServiceProvider();

        var requestDelegate = await GetRequestDelegate(source, services);

        using var requestScoped = services.CreateScope();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = requestScoped.ServiceProvider
        };

        await requestDelegate(httpContext);

        Assert.Same(myOriginalService, httpContext.Items["service"]);
    }

    public static object[][] FromServiceActions
    {
        get
        {
            return ImplicitFromServiceActions.Concat(ExplicitFromServiceActions).ToArray();
        }
    }

    public static object[][] FromFormActions(bool withAcceptsMetadata = false)
    {
        var implicitFromFormFile =
        $$"""
        app.MapPost("/fileupload", (IFormFile file) =>
        {
            return $"Uploaded {file.Name}";
        });
        """;

        var implicitFromFormCollection =
        $$"""
        app.MapPost("/formpost", (IFormCollection formCollection) =>
        {
            return $"Uploaded {formCollection.Count} files";
        });
        """;

        var explicitFromFormFile =
        $$"""
        app.MapPost("/fileupload", ([FromForm] IFormFile file) =>
        {
            return $"Uploaded {file.Name}";
        });
        """;

        var explicitFromFormCollection =
        $$"""
        app.MapPost("/formpost", ([FromForm] IFormCollection formCollection) =>
        {
            return $"Uploaded {formCollection.Count} files";
        });
        """;

        if (withAcceptsMetadata)
        {
            return new[]
            {
                new[] { (object)implicitFromFormFile, typeof(IFormFile), new[] { "multipart/form-data" }},
                new[] { (object)implicitFromFormCollection, typeof(IFormCollection), new[] { "multipart/form-data" }},
                new[] { (object)explicitFromFormFile, typeof(IFormFile), new[] { "multipart/form-data" }},
                new[] { (object)explicitFromFormCollection, typeof(IFormCollection), new[] { "multipart/form-data" }}
            };
        }

        return new[]
        {
            new[] { (object)implicitFromFormFile },
            new[] { (object)implicitFromFormCollection },
            new[] { (object)explicitFromFormFile },
            new[] { (object)explicitFromFormCollection }
        };
    }

    [Theory]
    [MemberData(nameof(ImplicitFromBodyActions), parameters: new object[] { true })]
    [MemberData(nameof(ExplicitFromBodyActions), parameters: new object[] { true })]
    [MemberData(nameof(FromFormActions), parameters: new object[] { true })]
    public async Task PopulatesAcceptsMetadataForRequestBody(string source, Type expectedType, string[] expectedContentTypes)
    {
        var endpoint = await GetEndpoint(source);

        var acceptsMetadata = endpoint.Metadata.GetMetadata<IAcceptsMetadata>();
        Assert.NotNull(acceptsMetadata);

        Assert.Equal(expectedType, acceptsMetadata.RequestType);
        Assert.Equal(expectedContentTypes, acceptsMetadata.ContentTypes);
    }

    [Theory]
    [MemberData(nameof(FromServiceActions))]
    public async Task DoesNotPopulateAcceptsMetadataForServices(string source)
    {
        var myOriginalService = new MyService();
        var serviceProvider = new ServiceCollection()
            .AddSingleton(myOriginalService)
            .AddSingleton<IMyService>(myOriginalService)
            .BuildServiceProvider();
        var endpoint = await GetEndpoint(source, serviceProvider);

        var acceptsMetadata = endpoint.Metadata.GetMetadata<IAcceptsMetadata>();
        Assert.Null(acceptsMetadata);
    }

    [Fact]
    public async Task SupportsNullableReferenceTypesInParameters()
    {
        var source = """
        app.MapGet("/", (string? nullableString, DateTime? nullableDateTime) => $"{nullableString} {nullableDateTime}");
        """;

        var endpoint = await GetEndpoint(source);
    }

    public static object?[][] FromQueryOptionality
    {
        get
        {
            var requiredQueryParam = """app.MapGet("/", (string name) => $"Hello {name}!");""";
            var defaultValueQueryParam = """
            string defaultValueQueryParam(string name = "DefaultName") => $"Hello {name}!";
            app.MapGet("/", defaultValueQueryParam);
            """;
            var nullableQueryParam = """app.MapGet("/", (string? name) => $"Hello {name}!");""";
            var requiredParseableQueryParam = """app.MapGet("/", (int age) => $"Age: {age}");""";
            var defaultValueParseableQueryParam = """
            string defaultValuePareseableQueryParam(int age = 12) => $"Age: {age}";
            app.MapGet("/", defaultValuePareseableQueryParam);
            """;
            var nullableQueryParseableParam = """app.MapGet("/", (int? age) => $"Age: {age}");""";

            return new[]
            {
                new object?[] { requiredQueryParam, "name", null, 400, null},
                new object?[] { requiredQueryParam, "name", "TestName", 200, "Hello TestName!" },
                new object?[] { defaultValueQueryParam, "name", null, 200, "Hello DefaultName!" },
                new object?[] { defaultValueQueryParam, "name", "TestName", 200, "Hello TestName!" },
                new object?[] { nullableQueryParam, "name", null, 200, "Hello !" },
                new object?[] { nullableQueryParam, "name", "TestName", 200, "Hello TestName!"},

                new object?[] { requiredParseableQueryParam, "age", null, 400, null},
                new object?[] { requiredParseableQueryParam, "age", "42", 200, "Age: 42" },
                new object?[] { defaultValueParseableQueryParam, "age", null, 200, "Age: 12" },
                new object?[] { defaultValueParseableQueryParam, "age", "42", 200, "Age: 42" },
                new object?[] { nullableQueryParseableParam, "age", null, 200, "Age: " },
                new object?[] { nullableQueryParseableParam, "age", "42", 200, "Age: 42"},
            };
        }
    }

    [Theory]
    [MemberData(nameof(FromQueryOptionality))]
    public async Task HandlesQueryParamOptionality(string source, string paramName, string? queryParam, int expectedStatusCode, string? expectedResponse)
    {
        var endpoint = await GetEndpoint(source);
        QueryString? query = queryParam is not null ? QueryString.Create(paramName, queryParam) : null;

        await AssertEndpointBehavior(
                endpoint,
                expectedResponse ?? string.Empty,
                expectedStatusCode,
                query: query);
    }

    public static object?[][] FromRouteOptionality
    {
        get
        {
            var requiredRouteParam = """app.MapGet("/{name}", (string name) => $"Hello {name}!");""";
            var defaultValueRouteParam = """
            string defaultValueQueryParam(string name = "DefaultName") => $"Hello {name}!";
            app.MapGet("/{name}", defaultValueQueryParam);
            """;
            var nullableRouteParam = """app.MapGet("/{name}", (string? name) => $"Hello {name}!");""";
            var requiredParseableRouteParam = """app.MapGet("/{age}", (int age) => $"Age: {age}");""";
            var defaultValueParseableRouteParam = """
            string defaultValuePareseableRouteParam(int age = 12) => $"Age: {age}";
            app.MapGet("/{age}", defaultValuePareseableRouteParam);
            """;
            var nullableQueryParseableParam = """app.MapGet("/{age}", (int? age) => $"Age: {age}");""";

            return new[]
            {
                new object?[] { requiredRouteParam, "name", null, 400, null},
                new object?[] { requiredRouteParam, "name", "TestName", 200, "Hello TestName!" },
                new object?[] { defaultValueRouteParam, "name", null, 200, "Hello DefaultName!" },
                new object?[] { defaultValueRouteParam, "name", "TestName", 200, "Hello TestName!" },
                new object?[] { nullableRouteParam, "name", null, 200, "Hello !" },
                new object?[] { nullableRouteParam, "name", "TestName", 200, "Hello TestName!"},

                new object?[] { requiredParseableRouteParam, "age", null, 400, null},
                new object?[] { requiredParseableRouteParam, "age", "42", 200, "Age: 42" },
                new object?[] { defaultValueParseableRouteParam, "age", null, 200, "Age: 12" },
                new object?[] { defaultValueParseableRouteParam, "age", "42", 200, "Age: 42" },
                new object?[] { nullableQueryParseableParam, "age", null, 200, "Age: " },
                new object?[] { nullableQueryParseableParam, "age", "42", 200, "Age: 42"},
            };
        }
    }

    [Theory]
    [MemberData(nameof(FromRouteOptionality))]
    public async Task HandlesRouteParamOptionality(string source, string paramName, string? routeParam, int expectedStatusCode, string? expectedResponse)
    {
        var endpoint = await GetEndpoint(source);
        var routeValue = routeParam is not null ? new RouteValueDictionary { { paramName, routeParam } } : null;

        await AssertEndpointBehavior(
                endpoint,
                expectedResponse ?? string.Empty,
                expectedStatusCode,
                routeValues: routeValue);
    }

    public static object?[][] FromBodyOptionality
    {
        get
        {
            var requiredBodyParam = $"""
            app.MapPost("/todo", ({typeof(Todo)} todo) => todo);
            """;

            var defaultValueBodyParam = $"""
            {typeof(Todo)}? getTodo({typeof(Todo)}? todo = default) => todo;
            app.MapPost("/todo", getTodo);
            """;

            var nullableBodyParam = $"""
            app.MapPost("/todo-1", ({typeof(Todo)}? todo) => todo);
            """;

            var disallowEmptyAndNonOptional = $"""
            app.MapPost("/todo-2", ([{typeof(FromBodyAttribute)}(AllowEmpty = false)] {typeof(Todo)} todo) => todo);
            """;
            
            var allowEmptyAndNonOptional = $"""
            app.MapPost("/todo-3", ([{typeof(FromBodyAttribute)}(AllowEmpty = true)] {typeof(Todo)} todo) => todo);
            """;
            
            var allowEmptyAndOptional = $"""
            app.MapPost("/todo-4", ([{typeof(FromBodyAttribute)}(AllowEmpty = true)] {typeof(Todo)}? todo) => todo);
            """;
            
            var disallowEmptyAndOptional = $"""
            app.MapPost("/todo-5", ([{typeof(FromBodyAttribute)}(AllowEmpty = false)] {typeof(Todo)}? todo) => todo);
            """;

            var todo = new Todo { Name = "Run tests" };

            return new[]
            {
                new object?[] { requiredBodyParam, null, 400 },
                new object?[] { requiredBodyParam, todo, 200 },
                new object?[] { defaultValueBodyParam, null, 200 },
                new object?[] { defaultValueBodyParam, todo, 200 },
                new object?[] { nullableBodyParam, null, 200 },
                new object?[] { nullableBodyParam, todo, 200 },
                new object?[] { disallowEmptyAndNonOptional, null, 400 },
                new object?[] { allowEmptyAndNonOptional, null, 200 },
                new object?[] { allowEmptyAndOptional, null, 200 },
                new object?[] { disallowEmptyAndOptional, null, 200 }
            };
        }
    }

    [Theory]
    [MemberData(nameof(FromBodyOptionality))]
    public async Task HandlesBodyParamOptionality(string source, Todo? bodyParam, int expectedStatusCode)
    {
        Console.WriteLine(source);
        var requestDelegate = await GetRequestDelegate(source);
        var httpContext = new DefaultHttpContext();

        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;

        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(bodyParam);
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;

        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var jsonOptions = new JsonOptions();
        jsonOptions.SerializerOptions.Converters.Add(new TodoJsonConverter());

        var mock = new Mock<IServiceProvider>();
        mock.Setup(m => m.GetService(It.IsAny<Type>())).Returns<Type>(t =>
        {
            if (t == typeof(IOptions<JsonOptions>))
            {
                return Options.Create(jsonOptions);
            }
            return null;
        });
        httpContext.RequestServices = mock.Object;

        await requestDelegate(httpContext);

        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);

        if (expectedStatusCode == 200)
        {
            var httpResponse = httpContext.Response;
            httpResponse.Body.Seek(0, SeekOrigin.Begin);
            var streamReader = new StreamReader(httpResponse.Body);
            var body = await streamReader.ReadToEndAsync();
            Assert.Equal(JsonSerializer.Serialize(bodyParam, options: new JsonSerializerOptions(JsonSerializerDefaults.Web)), body);
        }
    }

    public static object?[][] FromServiceOptionality
    {
        get
        {
            var requiredExplicitService = $"""
            void requiredExplicitService([{typeof(FromServiceAttribute)}]{typeof(MyService)} service, HttpContext httpContext) => httpContext.Items.Add("service", service);
            app.MapGet("/", requiredExplicitService);
            """;
            var defaultValueExplicitServiceParam = $"""
            void defaultValueExplicitServiceParam(HttpContext httpContext, [{typeof(FromServiceAttribute)}]{typeof(MyService)}? service = null) => httpContext.Items.Add("service", service);
            app.MapGet("/", defaultValueExplicitServiceParam);
            """;
            var nullableExplicitServiceParam = $"""
            app.MapGet("/", (HttpContext httpContext, [{typeof(FromServiceAttribute)}]{typeof(MyService)}? service) => httpContext.Items.Add("service", service));
            """;

            return new []
                {
                    new object?[] { requiredExplicitService, false, false},
                    new object?[] { requiredExplicitService, true, true},

                    new object?[] { defaultValueExplicitServiceParam, false, true},
                    new object?[] { defaultValueExplicitServiceParam, true, true},

                    new object?[] { nullableExplicitServiceParam, false, true},
                    new object?[] { nullableExplicitServiceParam, true, true},
                };
        }
    }

    [Theory]
    [MemberData(nameof(FromServiceOptionality))]
    public async Task HandleFromServiceOptionality(string source, bool hasService, bool isValid)
    {
        var myOriginalService = new MyService();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(myOriginalService);
        serviceCollection.AddSingleton<IMyService>(myOriginalService);
        var services = serviceCollection.BuildServiceProvider();

        var requestDelegate = await GetRequestDelegate(source, hasService ? services : null) ;

        using var requestScoped = services.CreateScope();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = hasService ? requestScoped.ServiceProvider : new ServiceCollection().BuildServiceProvider()
        };

        if (isValid)
        {
            await requestDelegate(httpContext);
            Assert.Same(hasService ? myOriginalService : null, httpContext.Items["service"]);
            Assert.Equal(200,httpContext.Response.StatusCode);
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => requestDelegate(httpContext));
            Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        }
    }
    
    public static IEnumerable<object?[]> BindAsyncOptionality
    {
        get
        {
            var requiredReferenceType = $"""
            app.MapPost("/1", (HttpContext context, {typeof(MyBindAsyncRecord)} myBindAsyncRecord) => context.Items["uri"] = myBindAsyncRecord.Uri);
            """;
            var defaultReferenceType = $"""
            void defaultReferenceTypeHandler(HttpContext context, {typeof(MyBindAsyncRecord)}? myBindAsyncRecord = null) => context.Items["uri"] = myBindAsyncRecord?.Uri;
            app.MapPost("/2", defaultReferenceTypeHandler);
            """;
            var nullableReferenceType = $"""
            app.MapPost("/3", (HttpContext context, {typeof(MyBindAsyncRecord)}? myBindAsyncRecord) => context.Items["uri"] = myBindAsyncRecord?.Uri);
            """;
            var requiredReferenceTypeSimple = $"""
            app.MapPost("/4", (HttpContext context, {typeof(MySimpleBindAsyncRecord)} mySimpleBindAsyncRecord) => context.Items["uri"] = mySimpleBindAsyncRecord.Uri);
            """;
            var requiredValueType = $"""
            app.MapPost("/5", (HttpContext context, {typeof(MyNullableBindAsyncStruct)} myNullableBindAsyncStruct) => context.Items["uri"] = myNullableBindAsyncStruct.Uri);
            """;
            var defaultValueType = $"""
            void defaultValueTypeHandler(HttpContext context, {typeof(MyNullableBindAsyncStruct)}? myNullableBindAsyncStruct = null) => context.Items["uri"] = myNullableBindAsyncStruct?.Uri;
            app.MapPost("/6", defaultValueTypeHandler);
            """;
            var nullableValueType = $"""
            app.MapPost("/7", (HttpContext context, {typeof(MyNullableBindAsyncStruct)}? myNullableBindAsyncStruct) => context.Items["uri"] = myNullableBindAsyncStruct?.Uri);
            """;
            var requiredValueTypeSimple = $"""
            app.MapPost("/8", (HttpContext context, {typeof(MySimpleBindAsyncStruct)} mySimpleBindAsyncStruct) => context.Items["uri"] = mySimpleBindAsyncStruct.Uri);
            """;

            return new object?[][]
            {
                    new object?[] { requiredReferenceType, false, true, false },
                    new object?[] { requiredReferenceType, true, false, false, },
                    new object?[] { requiredReferenceTypeSimple, true, false, false },
                    
                    new object?[] { defaultReferenceType, false, false, false, },
                    new object?[] { defaultReferenceType, true, false, false },
                    
                    new object?[] { nullableReferenceType, false, false, false },
                    new object?[] { nullableReferenceType, true, false, false },

                    new object?[] { requiredValueType, false, true, true },
                    new object?[] { requiredValueType, true, false, true },
                    new object?[] { requiredValueTypeSimple, true, false, true },

                    new object?[] { defaultValueType, false, false, true },
                    new object?[] { defaultValueType, true, false, true },
                    
                    new object?[] { nullableValueType, false, false, true },
                    new object?[] { nullableValueType, true, false, true },
            };
        }
    }
    
    [Theory]
    [MemberData(nameof(BindAsyncOptionality))]
    public async Task HandleBindAsyncOptionality(string source, bool includeReferer, bool isInvalid, bool isStruct)
    {
        // Arrange
        var requestDelegate = await GetRequestDelegate(source);
        var httpContext = new DefaultHttpContext();
        if (includeReferer)
        {
            httpContext.Request.Headers.Referer = "https://example.org";
        }

        // Assert
        if (!isInvalid)
        {
            await requestDelegate(httpContext);
            Assert.Equal(200, httpContext.Response.StatusCode);

            if (includeReferer)
            {
                Assert.Equal(new Uri("https://example.org"), httpContext.Items["uri"]);
            }
            else
            {
                Assert.Null(httpContext.Items["uri"]);
            }
        }
        else
        {
            await requestDelegate(httpContext);
            // await Assert.ThrowsAsync<InvalidOperationException>(() => requestDelegate(httpContext));
            Assert.Equal(400, httpContext.Response.StatusCode);
            Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        }
    }

    [Theory]
    [InlineData("""app.MapGet("/multiple-nn", (Microsoft.Extensions.Primitives.StringValues queries) => queries);""")]
    [InlineData("""app.MapGet("/multiple-n", (Microsoft.Extensions.Primitives.StringValues? queries) => queries);""")]
    public async Task HandleQueryHandlerTypeDifferentFromResolved(string source)
    {
        var requestDelegate = await GetRequestDelegate(source);

        var httpContext = new DefaultHttpContext();

        await requestDelegate(httpContext);
    }
    
    public async Task<Endpoint> GetEndpoint(string source, IServiceProvider? serviceProvider = null)
    {
        // Act
        var (results, compilation) = await RunGenerator(source);

        // Assert
        Assert.Empty(results.Diagnostics);

        var builderFunc = CreateInvocationFromCompilation(compilation);
        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider ?? new EmptyServiceProvider()));
        _ = builderFunc(builder);

        var dataSource = Assert.Single(builder.DataSources);
        // Trigger Endpoint build by calling getter.
        var endpoint = Assert.Single(dataSource.Endpoints);

        var sourceKeyMetadata = endpoint.Metadata.GetMetadata<SourceKey>();
        Assert.NotNull(sourceKeyMetadata);

        return endpoint;
    }

    private async Task<RequestDelegate> GetRequestDelegate(string source, IServiceProvider? serviceProvider = null)
    {
        var endpoint = await GetEndpoint(source, serviceProvider);
        Assert.NotNull(endpoint.RequestDelegate);
        return endpoint.RequestDelegate;
    }

    private static async Task AssertEndpointBehavior(
        Endpoint endpoint,
        string expectedResponse,
        int expectedStatusCode,
        IServiceProvider? serviceProvider = null,
        RouteValueDictionary? routeValues = null,
        QueryString? query = null)
    {
        var httpContext = new DefaultHttpContext();
        IServiceScope? scope = null;

        if (serviceProvider is not null)
        {
            scope = serviceProvider.CreateScope();
            httpContext.RequestServices = scope.ServiceProvider;
        }

        var outStream = new MemoryStream();
        httpContext.Response.Body = outStream;

        if (query is { } q)
        {
            httpContext.Request.QueryString = q;
        }

        if (routeValues is not null)
        {
            httpContext.Request.RouteValues = routeValues;
        }

        Assert.NotNull(endpoint.RequestDelegate);
        await endpoint.RequestDelegate(httpContext);

        var httpResponse = httpContext.Response;
        httpResponse.Body.Seek(0, SeekOrigin.Begin);
        var streamReader = new StreamReader(httpResponse.Body);
        var body = await streamReader.ReadToEndAsync();
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        Assert.Equal(expectedResponse, body);

        scope?.Dispose();
    }

    private static Func<IEndpointRouteBuilder, IEndpointRouteBuilder> CreateInvocationFromCompilation(Compilation compilation)
    {
        var assemblyName = compilation.AssemblyName!;
        var symbolsName = Path.ChangeExtension(assemblyName, "pdb");

        var output = new MemoryStream();
        var pdb = new MemoryStream();

        var emitOptions = new EmitOptions(
                        debugInformationFormat: DebugInformationFormat.PortablePdb,
                        pdbFilePath: symbolsName);

        var embeddedTexts = new List<EmbeddedText>();

        // Make sure we embed the sources in pdb for easy debugging
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var text = syntaxTree.GetText();
            var encoding = text.Encoding ?? Encoding.UTF8;
            var buffer = encoding.GetBytes(text.ToString());
            var sourceText = SourceText.From(buffer, buffer.Length, encoding, canBeEmbedded: true);

            var syntaxRootNode = (CSharpSyntaxNode)syntaxTree.GetRoot();
            var newSyntaxTree = CSharpSyntaxTree.Create(syntaxRootNode, options: null, encoding: encoding, path: syntaxTree.FilePath);

            compilation = compilation.ReplaceSyntaxTree(syntaxTree, newSyntaxTree);

            embeddedTexts.Add(EmbeddedText.FromSource(syntaxTree.FilePath, sourceText));
        }

        var result = compilation.Emit(output, pdb, options: emitOptions, embeddedTexts: embeddedTexts);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity > DiagnosticSeverity.Warning));
        Assert.True(result.Success);

        output.Position = 0;
        pdb.Position = 0;

        var assembly = AssemblyLoadContext.Default.LoadFromStream(output, pdb);
        var handler = assembly?.GetType("TestMapActions")
                       ?.GetMethod("MapTestEndpoints", BindingFlags.Public | BindingFlags.Static)
                       ?.CreateDelegate<Func<IEndpointRouteBuilder, IEndpointRouteBuilder>>();

        Assert.NotNull(handler);
        return handler;
    }

    private static async Task<(GeneratorRunResult, Compilation)> RunGenerator(string mapAction)
    {
        var project = CreateProject();
        var source = $$"""
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

public static class TestMapActions
{
    public static IEndpointRouteBuilder MapTestEndpoints(this IEndpointRouteBuilder app)
    {
        {{mapAction}}
        return app;
    }
}
""";
        project = project.AddDocument("TestMapActions.cs", SourceText.From(source, Encoding.UTF8)).Project;
        var compilation = await project.GetCompilationAsync();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new uControllerGenerator() },
            parseOptions: (CSharpParseOptions)project.ParseOptions!);

        Assert.NotNull(compilation);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var _);

        var results = driver.GetRunResult();
        var diagnostics = outputCompilation.GetDiagnostics();
        Assert.Empty(diagnostics.Where(d => d.Severity > DiagnosticSeverity.Warning));
        return (results.Results[0], outputCompilation);
    }

    private static Project CreateProject()
    {
        var projectName = $"TestProject-{Guid.NewGuid()}";
        var projectId = ProjectId.CreateNewId(projectName);

        var solution = new AdhocWorkspace()
           .CurrentSolution
           .AddProject(projectId, projectName, projectName, LanguageNames.CSharp);

        var project = solution.Projects.Single()
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithNullableContextOptions(NullableContextOptions.Enable))
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp11).WithPreprocessorSymbols("NET7_0_OR_GREATER"));

        var resolver = new AppLocalResolver();
        var dependencyContext = DependencyContext.Load(typeof(IntegrationTests).Assembly);

        Assert.NotNull(dependencyContext);

        foreach (var defaultCompileLibrary in dependencyContext.CompileLibraries)
        {
            foreach (var resolveReferencePath in defaultCompileLibrary.ResolveReferencePaths(resolver))
            {
                // Skip the source generator itself
                if (resolveReferencePath.Equals(typeof(uControllerGenerator).Assembly.Location))
                {
                    continue;
                }

                project = project.AddMetadataReference(MetadataReference.CreateFromFile(resolveReferencePath));
            }
        }

        return project;
    }

    private class RequestBodyDetectionFeature : IHttpRequestBodyDetectionFeature
    {
        public RequestBodyDetectionFeature(bool canHaveBody)
        {
            CanHaveBody = canHaveBody;
        }

        public bool CanHaveBody { get; }
    }

    private static IEndpointRouteBuilder CreateEndpointBuilder(IServiceProvider? serviceProvider = null)
    {
        return new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider ?? new EmptyServiceProvider()));
    }

    private class AppLocalResolver : ICompilationAssemblyResolver
    {
        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string>? assemblies)
        {
            foreach (var assembly in library.Assemblies)
            {
                var dll = Path.Combine(Directory.GetCurrentDirectory(), "refs", Path.GetFileName(assembly));
                if (File.Exists(dll))
                {
                    assemblies ??= new();
                    assemblies.Add(dll);
                    return true;
                }

                dll = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(assembly));
                if (File.Exists(dll))
                {
                    assemblies ??= new();
                    assemblies.Add(dll);
                    return true;
                }
            }

            return false;
        }
    }

    private class EmptyServiceProvider : IServiceScope, IServiceProvider, IServiceScopeFactory
    {
        public IServiceProvider ServiceProvider => this;

        public RouteHandlerOptions RouteHandlerOptions { get; set; } = new RouteHandlerOptions();

        public IServiceScope CreateScope()
        {
            return this;
        }

        public void Dispose() { }

        public object? GetService(Type serviceType)
        {
            return null;
        }
    }

    private class DefaultEndpointRouteBuilder : IEndpointRouteBuilder
    {
        public DefaultEndpointRouteBuilder(IApplicationBuilder applicationBuilder)
        {
            ApplicationBuilder = applicationBuilder ?? throw new ArgumentNullException(nameof(applicationBuilder));
            DataSources = new List<EndpointDataSource>();
        }

        public IApplicationBuilder ApplicationBuilder { get; }

        public IApplicationBuilder CreateApplicationBuilder() => ApplicationBuilder.New();

        public ICollection<EndpointDataSource> DataSources { get; }

        public IServiceProvider ServiceProvider => ApplicationBuilder.ApplicationServices;
    }
}