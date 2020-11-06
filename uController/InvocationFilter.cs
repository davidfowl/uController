using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace uController
{
    public abstract class InvocationFilter
    {
        public virtual ValueTask<object> InvokeMethodAsync(InvocationContext context, InvocationDelegate next) => next(context);
    }

    public readonly struct InvocationContext
    {
        InvocationContext(HttpContext httpContext, object[] arguments, object instance)
        {
            HttpContext = httpContext;
            Arguments = arguments;
            Instance = instance;
        }

        public HttpContext HttpContext { get; }
        public object[] Arguments { get; }
        public object Instance { get; }
    }

    public delegate ValueTask<object> InvocationDelegate(InvocationContext context);
}
