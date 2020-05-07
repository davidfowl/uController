using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace System.Reflection
{
    public class ParameterWrapper : ParameterInfo
    {
        private IParameterSymbol _parameter;

        public ParameterWrapper(IParameterSymbol p)
        {
            _parameter = p;
        }

        public override Type ParameterType => new TypeWrapper((INamedTypeSymbol)_parameter.Type);
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