using System;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace uController.SourceGenerator
{
    internal class WellKnownTypes
    {
        public WellKnownTypes(MetadataLoadContext metadataLoadContext)
        {
            // REVIEW: Consider making this lazy
            FromQueryAttributeType = metadataLoadContext.Resolve<FromQueryAttribute>();
            FromRouteAttributeType = metadataLoadContext.Resolve<FromRouteAttribute>();
            FromHeaderAttributeType = metadataLoadContext.Resolve<FromHeaderAttribute>();
            FromFormAttributeType = metadataLoadContext.Resolve<FromFormAttribute>();
            FromBodyAttributeType = metadataLoadContext.Resolve<FromBodyAttribute>();
            FromServicesAttributeType = metadataLoadContext.Resolve<FromServicesAttribute>();
            AsParametersAttributeType = metadataLoadContext.Resolve<AsParametersAttribute>();
            EndpointMetadataProviderType = metadataLoadContext.Resolve<IEndpointMetadataProvider>();
            EndpointRouteBuilderType = metadataLoadContext.Resolve<IEndpointRouteBuilder>();
            DelegateType = metadataLoadContext.Resolve<Delegate>();
            IResultType = metadataLoadContext.Resolve<IResult>();
            HttpContextType = metadataLoadContext.Resolve<HttpContext>();
            ParamterInfoType = metadataLoadContext.Resolve<ParameterInfo>();
            IFormatProviderType = metadataLoadContext.Resolve<IFormatProvider>();
        }

        public Type FromQueryAttributeType { get; }
        public Type FromRouteAttributeType { get; }
        public Type FromHeaderAttributeType { get; }
        public Type FromFormAttributeType { get; }
        public Type FromBodyAttributeType { get; }
        public Type FromServicesAttributeType { get; }
        public Type AsParametersAttributeType { get; }
        public Type EndpointMetadataProviderType { get; }
        public Type EndpointRouteBuilderType { get; }
        public Type DelegateType { get; }
        public Type IResultType { get; }
        public Type HttpContextType { get; }
        public Type ParamterInfoType { get; }
        public Type IFormatProviderType { get; }
    }
}
