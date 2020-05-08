using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace System.Reflection
{
    public class ParameterWrapper : ParameterInfo
    {
        private readonly IParameterSymbol _parameter;

        public ParameterWrapper(IParameterSymbol parameter)
        {
            _parameter = parameter;
        }

        public override Type ParameterType => _parameter.Type.AsType();
        public override string Name => _parameter.Name;

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            var attributes = new List<CustomAttributeData>();
            foreach (var a in _parameter.GetAttributes())
            {
                attributes.Add(new CustomAttributeDataWrapper(a));
            }
            return attributes;
        }
    }
}