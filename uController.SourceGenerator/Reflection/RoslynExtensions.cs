using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace System.Reflection
{
    public static class RoslynExtensions
    {
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
