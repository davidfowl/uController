using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace System.Reflection
{
    internal class TypeWrapper : Type
    {
        private readonly INamedTypeSymbol _namedTypeSymbol;

        public TypeWrapper(INamedTypeSymbol namedTypeSymbol)
        {
            _namedTypeSymbol = namedTypeSymbol;
        }

        public override Assembly Assembly => new AssemblyWrapper(_namedTypeSymbol.ContainingAssembly);

        public override string AssemblyQualifiedName => throw new NotImplementedException();

        public override Type BaseType => _namedTypeSymbol.BaseType.AsType();

        public override string FullName => Namespace == null ? Name : Namespace + "." + Name;

        public override Guid GUID => Guid.Empty;

        public override Module Module => throw new NotImplementedException();

        public override string Namespace => _namedTypeSymbol.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining));

        public override Type UnderlyingSystemType => throw new NotImplementedException();

        public override string Name => _namedTypeSymbol.MetadataName;

        public override bool IsGenericType => _namedTypeSymbol.IsGenericType;

        public override bool IsGenericTypeDefinition => base.IsGenericTypeDefinition;

        public override Type[] GetGenericArguments()
        {
            var args = new List<Type>();
            foreach (var item in _namedTypeSymbol.TypeArguments)
            {
                args.Add(item.AsType());
            }
            return args.ToArray();
        }

        public override Type GetGenericTypeDefinition()
        {
            return _namedTypeSymbol.ConstructedFrom.AsType();
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            var attributes = new List<CustomAttributeData>();
            foreach (var a in _namedTypeSymbol.GetAttributes())
            {
                attributes.Add(new CustomAttributeDataWrapper(a));
            }
            return attributes;
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            var ctors = new List<ConstructorInfo>();
            foreach (var c in _namedTypeSymbol.Constructors)
            {
                ctors.Add(new ConstructorInfoWrapper(c));
            }
            return ctors.ToArray();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException();
        }

        public override Type GetElementType()
        {
            if (_namedTypeSymbol is IArrayTypeSymbol array)
            {
                return array.ElementType.AsType();
            }

            return null;
        }

        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override Type GetInterface(string name, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        public override Type[] GetInterfaces()
        {
            var interfaces = new List<Type>();
            foreach (var i in _namedTypeSymbol.Interfaces)
            {
                interfaces.Add(new TypeWrapper(i));
            }
            return interfaces.ToArray();
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            var methods = new List<MethodInfo>();
            foreach (var m in _namedTypeSymbol.GetMembers())
            {
                // TODO: Efficiency
                if (m is IMethodSymbol method && !_namedTypeSymbol.Constructors.Contains(method))
                {
                    if ((bindingAttr & BindingFlags.Public) == BindingFlags.Public &&
                        (m.DeclaredAccessibility & Accessibility.Public) == Accessibility.Public)
                    {
                        methods.Add(new MethodInfoWrapper(method));
                    }
                }
            }
            return methods.ToArray();
        }

        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            var nestedTypes = new List<Type>();
            foreach (var type in _namedTypeSymbol.GetTypeMembers())
            {
                nestedTypes.Add(type.AsType());
            }
            return nestedTypes.ToArray();
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            var properties = new List<PropertyInfo>();
            foreach (var item in _namedTypeSymbol.GetMembers())
            {
                if (item is IPropertySymbol property)
                {
                    properties.Add(new PropertyWrapper(property));
                }
            }
            return properties.ToArray();
        }

        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
        {
            throw new NotSupportedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            throw new NotImplementedException();
        }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        protected override bool HasElementTypeImpl()
        {
            throw new NotImplementedException();
        }

        protected override bool IsArrayImpl()
        {
            return _namedTypeSymbol is IArrayTypeSymbol;
        }

        protected override bool IsByRefImpl()
        {
            throw new NotImplementedException();
        }

        protected override bool IsCOMObjectImpl()
        {
            throw new NotImplementedException();
        }

        protected override bool IsPointerImpl()
        {
            throw new NotImplementedException();
        }

        protected override bool IsPrimitiveImpl()
        {
            throw new NotImplementedException();
        }

        public override bool IsAssignableFrom(Type c)
        {
            if (c is TypeWrapper tr)
            {
                return tr._namedTypeSymbol.AllInterfaces.Contains(_namedTypeSymbol) || tr._namedTypeSymbol.BaseTypes().Contains(_namedTypeSymbol);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return _namedTypeSymbol.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (o is TypeWrapper tw)
            {
                return _namedTypeSymbol.Equals(tw._namedTypeSymbol, SymbolEqualityComparer.Default);
            }
            return base.Equals(o);
        }

        public override bool Equals(Type o)
        {
            if (o is TypeWrapper tw)
            {
                return _namedTypeSymbol.Equals(tw._namedTypeSymbol, SymbolEqualityComparer.Default);
            }
            return base.Equals(o);
        }
    }
}
