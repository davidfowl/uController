using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace System.Reflection
{
    public static class RoslynExtensions
    {
        public static Type AsType(this ITypeSymbol typeSymbol) => (typeSymbol as INamedTypeSymbol).AsType();

        public static Type AsType(this INamedTypeSymbol typeSymbol) => typeSymbol == null ? null : new TypeWrapper(typeSymbol);

        public static ParameterInfo AsParameterInfo(this IParameterSymbol parameterSymbol) => parameterSymbol == null ? null : new ParameterWrapper(parameterSymbol);

        public static MethodInfo AsMethodInfo(this IMethodSymbol methodSymbol) => methodSymbol == null ? null : new MethodInfoWrapper(methodSymbol);


        public static IEnumerable<INamedTypeSymbol> BaseTypes(this INamedTypeSymbol typeSymbol)
        {
            var t = typeSymbol;
            while (t != null)
            {
                yield return t;
                t = t.BaseType;
            }
        }
    }
}
