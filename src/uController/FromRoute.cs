using System;
using System.Collections.Generic;
using System.Text;

namespace uController
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class FromRouteAttribute : Attribute
    {
        public string Name { get; private set; }

        public FromRouteAttribute()
        {
        }

        public FromRouteAttribute(string name)
        {
            Name = name;
        }
    }
}
