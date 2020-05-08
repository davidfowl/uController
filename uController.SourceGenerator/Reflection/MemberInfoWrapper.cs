using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace System.Reflection
{
    internal class MemberInfoWrapper : MemberInfo
    {
        private readonly ISymbol _member;

        public MemberInfoWrapper(ISymbol member)
        {
            _member = member;
        }

        public override Type DeclaringType => _member.ContainingType.AsType();

        public override MemberTypes MemberType => throw new NotImplementedException();

        public override string Name => _member.Name;

        public override Type ReflectedType => throw new NotImplementedException();

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            var attributes = new List<CustomAttributeData>();
            foreach (var a in _member.GetAttributes())
            {
                attributes.Add(new CustomAttributeDataWrapper(a));
            }
            return attributes;
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
    }
}