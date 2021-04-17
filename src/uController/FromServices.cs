using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http.Metadata;

namespace uController
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class FromServicesAttribute : Attribute, IFromServiceMetadata
    {
    }
}
