using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis;

namespace System.Reflection
{
    internal class ConstructorInfoWrapper : ConstructorInfo
    {
        private IMethodSymbol _ctor;

        public ConstructorInfoWrapper(IMethodSymbol ctor)
        {
            _ctor = ctor;
        }

        public override Type DeclaringType => new TypeWrapper(_ctor.ContainingType);

        public override MethodAttributes Attributes => throw new NotImplementedException();

        public override RuntimeMethodHandle MethodHandle => throw new NotImplementedException();

        public override string Name => _ctor.Name;

        public override Type ReflectedType => throw new NotImplementedException();

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            var attributes = new List<CustomAttributeData>();
            foreach (var a in _ctor.GetAttributes())
            {
                attributes.Add(new CustomAttributeDataWrapper(a));
            }
            return attributes;
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetParameters()
        {
            var parameters = new List<ParameterInfo>();
            foreach (var p in _ctor.Parameters)
            {
                parameters.Add(new ParameterWrapper(p));
            }
            return parameters.ToArray();
        }

        public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
    }
}