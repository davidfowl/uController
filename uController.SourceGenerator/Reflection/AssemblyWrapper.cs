using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace System.Reflection
{
    internal class AssemblyWrapper : Assembly
    {
        private IAssemblySymbol _assembly;

        public AssemblyWrapper(IAssemblySymbol assembly)
        {
            _assembly = assembly;
        }

        public override Type[] GetExportedTypes()
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
            return new TypeWrapper(_assembly.GetTypeByMetadataName(name));
        }
    }
}
