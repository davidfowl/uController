using System;
using System.Collections.Generic;
using System.Text;

namespace Web.Framework
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class FromQueryAttribute : Attribute
    {
        public string Name { get; private set; }

        public FromQueryAttribute()
        {
        }

        public FromQueryAttribute(string name)
        {
            Name = name;
        }
    }
}
