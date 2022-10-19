using System;
using System.Collections.Generic;
using System.Reflection;

namespace uController
{
    public class MethodModel
    {
        public string UniqueName { get; set; }
        public MethodInfo MethodInfo { get; set; }
        public List<ParameterModel> Parameters { get; } = new List<ParameterModel>();
        public List<object> Metadata { get; } = new List<object>();
        public string RoutePattern { get; set; }
    }

    public class ParameterModel
    {
        public string Name { get; set; }
        public Type ParameterType { get; set; }
        public string FromQuery { get; set; }
        public string FromHeader { get; set; }
        public string FromForm { get; set; }
        public string FromRoute { get; set; }
        public string FromCookie { get; set; }
        public bool FromBody { get; set; }
        public bool FromServices { get; set; }

        public bool HasBindingSource => FromBody || FromServices || FromCookie != null ||
            FromForm != null || FromQuery != null || FromHeader != null || FromRoute != null;

        public bool Unresovled { get; set; }
    }
}
