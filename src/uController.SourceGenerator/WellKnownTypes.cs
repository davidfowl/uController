using System;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Roslyn.Reflection;

namespace uController.SourceGenerator
{
    internal class WellKnownTypes
    {
        public WellKnownTypes(MetadataLoadContext metadataLoadContext)
        {
            // REVIEW: Consider making this lazy
            FromQueryAttributeType = metadataLoadContext.ResolveType<FromQueryAttribute>();
            FromRouteAttributeType = metadataLoadContext.ResolveType<FromRouteAttribute>();
            FromHeaderAttributeType = metadataLoadContext.ResolveType<FromHeaderAttribute>();
            FromFormAttributeType = metadataLoadContext.ResolveType<FromFormAttribute>();
            FromBodyAttributeType = metadataLoadContext.ResolveType<FromBodyAttribute>();
            FromServicesAttributeType = metadataLoadContext.ResolveType<FromServicesAttribute>();
            AsParametersAttributeType = metadataLoadContext.ResolveType<AsParametersAttribute>();
            IEndpointMetadataProviderType = metadataLoadContext.ResolveType<IEndpointMetadataProvider>();
            IEndpointParameterMetadataProviderType = metadataLoadContext.ResolveType<IEndpointParameterMetadataProvider>();
            EndpointRouteBuilderType = metadataLoadContext.ResolveType<IEndpointRouteBuilder>();
            DelegateType = metadataLoadContext.ResolveType<Delegate>();
            IResultType = metadataLoadContext.ResolveType<IResult>();
            HttpContextType = metadataLoadContext.ResolveType<HttpContext>();
            ParamterInfoType = metadataLoadContext.ResolveType<ParameterInfo>();
            IFormatProviderType = metadataLoadContext.ResolveType<IFormatProvider>();
            EnumType = metadataLoadContext.ResolveType<Enum>();
            ResultsType = metadataLoadContext.ResolveType<Results>();
            TypedResultsType = metadataLoadContext.ResolveType<TypedResults>();
        }

        public Type FromQueryAttributeType { get; }
        public Type FromRouteAttributeType { get; }
        public Type FromHeaderAttributeType { get; }
        public Type FromFormAttributeType { get; }
        public Type FromBodyAttributeType { get; }
        public Type FromServicesAttributeType { get; }
        public Type AsParametersAttributeType { get; }
        public Type IEndpointMetadataProviderType { get; }
        public Type IEndpointParameterMetadataProviderType { get; }
        public Type EndpointRouteBuilderType { get; }
        public Type DelegateType { get; }
        public Type IResultType { get; }
        public Type HttpContextType { get; }
        public Type ParamterInfoType { get; }
        public Type IFormatProviderType { get; }
        public Type EnumType { get; }
        public Type ResultsType { get; }
        public Type TypedResultsType { get; }
    }
}
