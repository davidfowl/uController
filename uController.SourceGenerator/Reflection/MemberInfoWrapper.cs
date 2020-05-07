using Microsoft.CodeAnalysis;

namespace System.Reflection
{
    internal class MemberInfoWrapper : MemberInfo
    {
        private ISymbol _member;

        public MemberInfoWrapper(ISymbol member)
        {
            _member = member;
        }

        public override Type DeclaringType => throw new NotImplementedException();

        public override MemberTypes MemberType => throw new NotImplementedException();

        public override string Name => _member.Name;

        public override Type ReflectedType => throw new NotImplementedException();

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
    }
}