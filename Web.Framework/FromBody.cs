using System;
using System.Collections.Generic;
using System.Text;

namespace Web.Framework
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class FromBodyAttribute : Attribute
    {
    }
}
