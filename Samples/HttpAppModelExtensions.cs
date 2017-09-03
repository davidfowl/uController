using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web.Framework;

namespace Samples
{
    public static class HttpAppModelExtensions
    {
        public static HttpModel MapRouteParametersToMethodArguments(this HttpModel model)
        {
            foreach (var m in model.Methods)
            {
                m.MapRouteParameters();
            }
            return model;
        }

        public static MethodModel MapRouteParameters(this MethodModel model)
        {
            if (model.RouteTemplate == null)
            {
                // No route, nothing to do here
                return null;
            }

            foreach (var p in model.Parameters)
            {
                if (p.FromBody ||
                    p.FromServices ||
                    p.FromCookie != null ||
                    p.FromForm != null ||
                    p.FromQuery != null ||
                    p.FromHeader != null ||
                    p.FromRoute != null)
                {
                    // Something set, don't override
                    continue;
                }

                var part = model.RouteTemplate.GetParameter(p.Name);
                if (part != null)
                {
                    p.FromRoute = part.Name;
                }
            }

            return model;
        }
    }
}
