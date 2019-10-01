using Microsoft.Extensions.DependencyInjection;
using uController;

namespace Samples
{
    public static class JsonExtensions
    {
        public static IServiceCollection AddJson(this IServiceCollection services)
        {
            return services.AddSingleton<IHttpRequestReader, JsonRequestReader>()
                           .AddSingleton<IHttpResponseWriter, JsonResponseWriter>();
        }
    }
}
