using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace System.Reflection
{
    public static class RoslynExtensions
    {
        public static Type AsType(this ITypeSymbol typeSymbol, MetadataLoadContext metadataLoadContext) => typeSymbol == null ? null : new TypeWrapper(typeSymbol, metadataLoadContext);

        public static ParameterInfo AsParameterInfo(this IParameterSymbol parameterSymbol, MetadataLoadContext metadataLoadContext) => parameterSymbol == null ? null : new ParameterWrapper(parameterSymbol, metadataLoadContext);

        public static MethodInfo AsMethodInfo(this IMethodSymbol methodSymbol, MetadataLoadContext metadataLoadContext) => methodSymbol == null ? null : new MethodInfoWrapper(methodSymbol, metadataLoadContext);

        public static IMethodSymbol GetMethodSymbol(this MethodInfo methodInfo) => (methodInfo as MethodInfoWrapper)?.MethodSymbol;

        public static IPropertySymbol GetPropertySymbol(this PropertyInfo property) => (property as PropertyWrapper)?.PropertySymbol;

        public static IParameterSymbol GetParameterSymbol(this ParameterInfo parameterInfo) => (parameterInfo as ParameterWrapper)?.ParameterSymbol;

        public static ITypeSymbol GetTypeSymbol(this Type type) => (type as TypeWrapper)?.TypeSymbol;

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
