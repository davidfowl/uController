using Microsoft.CodeAnalysis;

namespace System.Reflection
{
    public class MetadataLoadContext
    {
        private readonly Compilation _compilation;

        public MetadataLoadContext(Compilation compilation)
        {
            _compilation = compilation;
        }

        public Type Resolve(string fullyQualifiedMetadataName)
        {
            return _compilation.GetTypeByMetadataName(fullyQualifiedMetadataName)?.AsType(this);
        }

        public Type Resolve<T>() => Resolve(typeof(T));

        public Type Resolve(Type type)
        {
            var resolvedType = _compilation.GetTypeByMetadataName(type.FullName);

            if (resolvedType is not null)
            {
                return resolvedType.AsType(this);
            }

            if (type.IsArray)
            {
                var typeSymbol = _compilation.GetTypeByMetadataName(type.GetElementType().FullName);
                if (typeSymbol == null)
                {
                    return null;
                }

                return _compilation.CreateArrayTypeSymbol(typeSymbol).AsType(this);
            }

            return null;
        }
    }
}
