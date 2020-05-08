// This class is a helper with the right type names

using System;
using System.Runtime.CompilerServices;

namespace uController
{
    [TypeForwardedFrom("uController")]
    public class HttpHandler { }
    [TypeForwardedFrom("uController")]
    public class IHttpRequestReader { }
    [TypeForwardedFrom("uController")]
    public class Result { }
    [TypeForwardedFrom("uController")]
    public class ObjectResult { }
    [TypeForwardedFrom("uController")]
    public class RouteAttribute { }
    [TypeForwardedFrom("uController")]
    public class HttpMethodAttribute { }
    [TypeForwardedFrom("uController")]
    public class FromQueryAttribute { }
    [TypeForwardedFrom("uController")]
    public class FromHeaderAttribute { }
    [TypeForwardedFrom("uController")]
    public class FromBodyAttribute { }
    [TypeForwardedFrom("uController")]
    public class FromFormAttribute { }
    [TypeForwardedFrom("uController")]
    public class FromRouteAttribute { }
    [TypeForwardedFrom("uController")]
    public class FromCookieAttribute { }
    [TypeForwardedFrom("uController")]
    public class FromServicesAttribute { }
    [TypeForwardedFrom("uController")]
    public class JsonRequestReader { }
    [TypeForwardedFrom("uController")]
    public class JsonResponseWriter { }
}

namespace Microsoft.AspNetCore.Http
{
    [TypeForwardedFrom("Microsoft.AspNetCore.Http.Abstractions")]
    public class HttpContext { }
    [TypeForwardedFrom("Microsoft.AspNetCore.Http.Features")]
    public interface IFormCollection { }
}

namespace Microsoft.AspNetCore.Routing
{
    [TypeForwardedFrom("Microsoft.AspNetCore.Routing")]
    public interface IEndpointRouteBuilder { }
}

namespace Microsoft.Extensions.DependencyInjection
{
    [TypeForwardedFrom("Microsoft.Extensions.DependencyInjection.Abstractions")]
    public delegate object ObjectFactory(IServiceProvider serviceProvider, object[] arguments);
    [TypeForwardedFrom("Microsoft.Extensions.DependencyInjection.Abstractions")]
    public static class ActivatorUtilities { }
}

namespace System.Text.Json
{
    [TypeForwardedFrom("System.Text.Json")]
    public struct JsonElement { }
}