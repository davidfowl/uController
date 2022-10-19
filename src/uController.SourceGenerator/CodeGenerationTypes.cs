// This class is a helper with the right type names

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Http
{
    [TypeForwardedFrom("Microsoft.AspNetCore.Http.Abstractions")]
    public interface IResult { }
    [TypeForwardedFrom("Microsoft.AspNetCore.Http.Abstractions")]
    public class HttpContext { }
    [TypeForwardedFrom("Microsoft.AspNetCore.Http.Abstractions")]
    public class HttpRequest { }
    [TypeForwardedFrom("Microsoft.AspNetCore.Http.Abstractions")]
    public class HttpResponse { }
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