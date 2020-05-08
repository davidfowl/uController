using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace System.Reflection
{
    internal class AssemblyWrapper : Assembly
    {
        private readonly IAssemblySymbol _assembly;

        public AssemblyWrapper(IAssemblySymbol assembly)
        {
            _assembly = assembly;
        }

        public override Type[] GetExportedTypes()
        {
            return GetTypes();
        }

        public override Type[] GetTypes()
        {
            var types = new List<Type>();
            var stack = new Stack<INamespaceSymbol>();
            stack.Push(_assembly.GlobalNamespace);
            while (stack.Count > 0)
            {
                var current = stack.Pop();

                foreach (var type in current.GetTypeMembers())
                {
                    types.Add(new TypeWrapper(type));
                }

                foreach (var ns in current.GetNamespaceMembers())
                {
                    stack.Push(ns);
                }
            }
            return types.ToArray();
        }

        public override Type GetType(string name)
        {
            return _assembly.GetTypeByMetadataName(name).AsType();
        }
    }
}
