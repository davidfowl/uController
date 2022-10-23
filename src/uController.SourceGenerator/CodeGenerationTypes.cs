// This class is a helper with the right type names

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Http
{
    public interface IResult { }
    public class HttpContext { }
    public class HttpRequest { }
    public class HttpResponse { }
    public interface IFormCollection { }
}

namespace Microsoft.AspNetCore.Mvc
{
    public class FromQueryAttribute { }
    public class FromRouteAttribute { }
    public class FromHeaderAttribute { }
    public class FromFormAttribute { }
    public class FromBodyAttribute { }
    public class FromServicesAttribute { }
}

namespace Microsoft.AspNetCore.Http.Metadata
{

    public interface IEndpointMetadataProvider { }
}

namespace Microsoft.Extensions.Primitives
{
    public struct StringValues { }
}

namespace System.IO.Pipelines
{
    public class PipeReader { }
}
