using System;

namespace Microsoft.AspNetCore.Routing
{
    public static class EndpointRouteBuilderExtensions
    {
        public static void MapHttpHandler<THttpHandler>(this IEndpointRouteBuilder builder)
        {
            throw new NotSupportedException("This implementation is not supposed to run! Did the source generator work?");
        }
    }
}
