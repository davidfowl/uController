// This class is a helper with the right type names

namespace Microsoft.AspNetCore.Http
{
    internal interface IResult { }
    internal class HttpContext { }
    internal class HttpRequest { }
    internal class HttpResponse { }
    internal interface IFormCollection { }
    internal interface IFormFile { }
    internal class AsParametersAttribute { }
}

namespace Microsoft.AspNetCore.Mvc
{
    internal class FromQueryAttribute { }
    internal class FromRouteAttribute { }
    internal class FromHeaderAttribute { }
    internal class FromFormAttribute { }
    internal class FromBodyAttribute { }
    internal class FromServicesAttribute { }
}

namespace Microsoft.AspNetCore.Http.Metadata
{

    internal interface IEndpointMetadataProvider { }
}

namespace Microsoft.Extensions.Primitives
{
    internal struct StringValues { }
}

namespace System.IO.Pipelines
{
    internal class PipeReader { }
}

namespace Microsoft.AspNetCore.Routing
{
    internal interface IEndpointRouteBuilder { }
}