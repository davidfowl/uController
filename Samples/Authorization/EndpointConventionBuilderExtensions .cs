// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointConventionBuilderExtensions
    {
        public static IEndpointConventionBuilder RequireAuthorization(this IEndpointConventionBuilder builder, params string[] roles)
        {
            builder.Apply(endpointBuilder => endpointBuilder.Metadata.Add(new AuthorizeAttribute
            {
                Roles = string.Join(",", roles)
            }));
            return builder;
        }
    }
}