using System;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Roslyn.Reflection;

namespace uController.SourceGenerator
{
    internal class WellKnownTypes
    {
        public WellKnownTypes(MetadataLoadContext metadataLoadContext)
        {
            // REVIEW: Consider making this lazy
            FromQueryMetadataType = metadataLoadContext.ResolveType<IFromQueryMetadata>();
            FromRouteMetadataType = metadataLoadContext.ResolveType<IFromRouteMetadata>();
            FromHeaderMetadataType = metadataLoadContext.ResolveType<IFromHeaderMetadata>();
            FromFormMetadataType = metadataLoadContext.ResolveType<IFromFormMetadata>();
            FromBodyMetadataType = metadataLoadContext.ResolveType<IFromBodyMetadata>();
            FromServicesMetadataType = metadataLoadContext.ResolveType<IFromServiceMetadata>();
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
            SourceKeyType = metadataLoadContext.ResolveType("Microsoft.AspNetCore.Builder.SourceKey");
        }

        public Type FromQueryMetadataType { get; }
        public Type FromRouteMetadataType { get; }
        public Type FromHeaderMetadataType { get; }
        public Type FromFormMetadataType { get; }
        public Type FromBodyMetadataType { get; }
        public Type FromServicesMetadataType { get; }
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
        public Type SourceKeyType { get; }
    }
}
