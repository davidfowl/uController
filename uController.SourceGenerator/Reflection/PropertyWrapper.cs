using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis;

namespace System.Reflection
{
    internal class PropertyWrapper : PropertyInfo
    {
        private readonly IPropertySymbol _property;

        public PropertyWrapper(IPropertySymbol property)
        {
            _property = property;
        }

        public override PropertyAttributes Attributes => throw new NotImplementedException();

        public override bool CanRead => _property.GetMethod != null;

        public override bool CanWrite => _property.SetMethod != null;

        public override Type PropertyType => _property.Type.AsType();

        public override Type DeclaringType => _property.ContainingType.AsType();

        public override string Name => _property.Name;

        public override Type ReflectedType => throw new NotImplementedException();

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException();
        }

        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            return _property.GetMethod.AsMethodInfo();
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            var parameters = new List<ParameterInfo>();
            foreach (var p in _property.Parameters)
            {
                parameters.Add(new ParameterWrapper(p));
            }
            return parameters.ToArray();
        }

        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            return _property.SetMethod.AsMethodInfo();
        }

        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}