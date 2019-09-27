using System;
using System.Collections.Generic;
using System.Text;

namespace uController
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class RouteAttribute : Attribute
    {
        public RouteAttribute(string template)
        {
            Template = template;
        }

        public string Template { get; private set; }
    }
}
