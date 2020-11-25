using System;
using System.Collections.Generic;
using System.Text;

namespace uController
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class FromCookieAttribute : Attribute
    {
        public string Name { get; private set; }

        public FromCookieAttribute()
        {

        }

        public FromCookieAttribute(string name)
        {
            Name = name;
        }
    }
}
