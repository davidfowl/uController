using System;
using Microsoft.AspNetCore.Http.Metadata;

namespace uController
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class FromBodyAttribute : Attribute, IFromBodyMetadata
    {
    }
}
