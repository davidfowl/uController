using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http.Metadata;

namespace uController
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class FromHeaderAttribute : Attribute, IFromHeaderMetadata
    {
        public string Name { get; private set; }

        public FromHeaderAttribute()
        {

        }

        public FromHeaderAttribute(string name)
        {
            Name = name;
        }
    }
}
