using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using uController.SourceGenerator;

namespace uController
{
    class MethodModel
    {
        public string UniqueName { get; set; }
        public MethodInfo MethodInfo { get; set; }
        public List<ParameterModel> Parameters { get; } = new List<ParameterModel>();
        public List<object> Metadata { get; } = new List<object>();
        public RoutePattern RoutePattern { get; set; }
        public bool DisableInferBodyFromParameters { get; set; }
    }

    class ParameterModel
    {
        public MethodModel Method { get; set; }
        public IParameterSymbol ParameterSymbol { get; set; }
        public ParameterInfo ParameterInfo { get; set; }
        public string Name { get; set; }
        public string GeneratedName { get; set; }
        public Type ParameterType { get; set; }
        public string FromQuery { get; set; }
        public string FromHeader { get; set; }
        public string FromForm { get; set; }
        public string FromRoute { get; set; }
        public bool FromBody { get; set; }
        public bool FromServices { get; set; }
        public int Index { get; set; }

        public bool HasBindingSource => FromBody || FromServices || 
            FromForm != null || FromQuery != null || FromHeader != null || FromRoute != null;

        public bool Unresovled { get; set; }

        public bool QueryOrRoute { get; set; }
        public bool BodyOrService { get; set; }
        public bool RequiresParameterInfo { get; set; }
        public bool ReadFromForm { get; set; }
    }
}
