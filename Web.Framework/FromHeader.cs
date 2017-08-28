using System;
using System.Collections.Generic;
using System.Text;

namespace Web.Framework
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class FromHeaderAttribute : Attribute
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
