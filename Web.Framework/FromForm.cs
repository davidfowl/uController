using System;
using System.Collections.Generic;
using System.Text;

namespace Web.Framework
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class FromFormAttribute : Attribute
    {
        public string Name { get; private set; }

        public FromFormAttribute()
        {

        }

        public FromFormAttribute(string name)
        {
            Name = name;
        }
    }
}
