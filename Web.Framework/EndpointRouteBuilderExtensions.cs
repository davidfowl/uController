using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Web.Framework
{
    public static class EndpointRouteBuilderExtensions
    {
        public static void MapHttpHandler<THttpHandler>(this IEndpointRouteBuilder builder)
        {
            var endpoints = HttpHandler.Build<THttpHandler>(builder.ServiceProvider);

            var dataSource = builder.DataSources.OfType<HandlerEndpointsDataSource>().SingleOrDefault();
            if (dataSource == null)
            {
                dataSource = new HandlerEndpointsDataSource();
                builder.DataSources.Add(dataSource);
            }

            dataSource.AddEndpoints(endpoints);
        }

        private class HandlerEndpointsDataSource : EndpointDataSource
        {
            private readonly List<Endpoint> _endpoints = new List<Endpoint>();

            public void AddEndpoints(IList<Endpoint> endpoints)
            {
                _endpoints.AddRange(endpoints);
            }

            public override IReadOnlyList<Endpoint> Endpoints => _endpoints;

            public override IChangeToken GetChangeToken()
            {
                return NullChangeToken.Singleton;
            }
        }
    }
}
