// This class is a helper with the right type names

namespace Microsoft.AspNetCore.Http
{
    interface IResult { }
    class HttpContext { }
    class HttpRequest { }
    class HttpResponse { }
    interface IFormCollection { }
    interface IFormFile { }
    class AsParametersAttribute { }
    class Results { }
    class TypedResults { }
}

namespace Microsoft.AspNetCore.Http.Metadata
{

    interface IEndpointMetadataProvider { }

    interface IEndpointParameterMetadataProvider { }

    interface IFromServiceMetadata { }
    interface IFromQueryMetadata { }
    interface IFromRouteMetadata { }
    interface IFromBodyMetadata
    {
        public bool AllowEmpty { get; }
    }
    interface IFromFormMetadata { }
    interface IFromHeaderMetadata { }
}

namespace Microsoft.Extensions.Primitives
{
    struct StringValues { }
}

namespace System.IO.Pipelines
{
    class PipeReader { }
}

namespace Microsoft.AspNetCore.Routing
{
    interface IEndpointRouteBuilder { }
}