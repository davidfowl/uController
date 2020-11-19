using System;

namespace uController
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class HttpHandlerAttribute : Attribute
    {
    }
}
