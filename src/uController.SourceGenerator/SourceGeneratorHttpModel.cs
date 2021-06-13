using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace uController
{
    public class SourceGeneratorHttpModel : HttpModel
    {
        public SourceGeneratorHttpModel(Type handlerType)
            : base(handlerType)
        {
        }

        public static HttpModel FromType(Type type, INamedTypeSymbol typeSymbol, SemanticModel semanticModel, Assembly uControllerAssembly)
        {
            var model = new HttpModel(type);

            var routeAttributeType = uControllerAssembly.GetType(typeof(RouteAttribute).FullName);
            var httpMethodAttributeType = uControllerAssembly.GetType(typeof(HttpMethodAttribute).FullName);
            var fromQueryAttributeType = uControllerAssembly.GetType(typeof(FromQueryAttribute).FullName);
            var fromHeaderAttributeType = uControllerAssembly.GetType(typeof(FromHeaderAttribute).FullName);
            var fromFormAttributeType = uControllerAssembly.GetType(typeof(FromFormAttribute).FullName);
            var fromBodyAttributeType = uControllerAssembly.GetType(typeof(FromBodyAttribute).FullName);
            var fromRouteAttributeType = uControllerAssembly.GetType(typeof(FromRouteAttribute).FullName);
            var fromCookieAttributeType = uControllerAssembly.GetType(typeof(FromCookieAttribute).FullName);
            var fromServicesAttributeType = uControllerAssembly.GetType(typeof(FromServicesAttribute).FullName);

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

            var routeAttribute = type.GetCustomAttributeData(routeAttributeType);
            var methodNames = new Dictionary<string, int>();

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttributeData(httpMethodAttributeType);
                var template = CombineRoute(routeAttribute?.GetConstructorArgument<string>(0), attribute?.GetConstructorArgument<string>(0) ?? method.GetCustomAttributeData(routeAttributeType)?.GetConstructorArgument<string>(0));

                var methodSymbol = GetMethodSymbol(typeSymbol, method, semanticModel);

                if (template == null)
                {
                    continue;
                }

                var methodModel = new MethodModel
                {
                    MethodInfo = method,
                    RoutePattern = template
                };

                if (!methodNames.TryGetValue(method.Name, out var count))
                {
                    methodNames[method.Name] = 1;
                    methodModel.UniqueName = method.Name;
                }
                else
                {
                    methodNames[method.Name] = count + 1;
                    methodModel.UniqueName = $"{method.Name}_{count}";
                }

                foreach (var metadata in method.CustomAttributes)
                {
                    if (metadata.AttributeType.Namespace == "System.Runtime.CompilerServices" ||
                        metadata.AttributeType.Name == "DebuggerStepThroughAttribute")
                    {
                        continue;
                    }
                    methodModel.Metadata.Add(metadata);
                }

                foreach (var parameter in method.GetParameters())
                {
                    var fromQuery = parameter.GetCustomAttributeData(fromQueryAttributeType);
                    var fromHeader = parameter.GetCustomAttributeData(fromHeaderAttributeType);
                    var fromForm = parameter.GetCustomAttributeData(fromFormAttributeType);
                    var fromBody = parameter.GetCustomAttributeData(fromBodyAttributeType);
                    var fromRoute = parameter.GetCustomAttributeData(fromRouteAttributeType);
                    var fromCookie = parameter.GetCustomAttributeData(fromCookieAttributeType);
                    var fromService = parameter.GetCustomAttributeData(fromServicesAttributeType);

                    var parameterSymbol = methodSymbol?.Parameters.FirstOrDefault(p => p.Name == parameter.Name);

                    methodModel.Parameters.Add(new ParameterModel
                    {
                        Name = parameter.Name,
                        ParameterType = parameter.ParameterType,
                        FromQuery = fromQuery == null ? null : fromQuery?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromHeader = fromHeader == null ? null : fromHeader?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromForm = fromForm == null ? null : fromForm?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromRoute = fromRoute == null ? null : fromRoute?.GetConstructorArgument<string>(0) ?? parameter.Name,
                        FromCookie = fromCookie == null ? null : fromCookie?.GetConstructorArgument<string>(0),
                        FromBody = fromBody != null,
                        FromServices = fromService != null,
                        DefaultValue = parameterSymbol?.HasExplicitDefaultValue ?? false ? parameterSymbol.ExplicitDefaultValue : null
                    });
                }

                model.Methods.Add(methodModel);
            }

            return model;
        }
    
        private static IMethodSymbol GetMethodSymbol(INamedTypeSymbol typeSymbol, MethodInfo method, SemanticModel semanticModel)
        {
            var methodSymbols = typeSymbol
                .GetMembers(method.Name)
                .OfType<IMethodSymbol>();

            foreach (var methodSymbol in methodSymbols)
            {
                if (DoReturnTypeMatch(methodSymbol, method, semanticModel) && DoParamatersMatch(methodSymbol, method, semanticModel))
                {
                    return methodSymbol;
                }
            }

            return null;
        }

        private static bool DoParamatersMatch(IMethodSymbol methodSymbol, MethodInfo method, SemanticModel semanticModel)
        {
            var parameterInfo = method.GetParameters();
            var parameterSymbols = methodSymbol.Parameters;

            if (parameterInfo.Length == 0 && parameterSymbols.Length ==0)
            {
                return true;
            }

            if (parameterInfo.Length != parameterSymbols.Length)
            {
                return false;
            }

            var parameterTypes = parameterInfo.Select(p => p.ParameterType).ToArray();
            var parameterTypeSymbols = parameterSymbols.Select(p => p.Type).ToArray();

            for (var i = 0; i < parameterTypes.Length; i++)
            {
                if (!IsSameType(parameterTypes[i], parameterTypeSymbols[i], semanticModel))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool DoReturnTypeMatch(IMethodSymbol methodSymbol, MethodInfo method, SemanticModel semanticModel)
        {
            return IsSameType(method.ReturnType, methodSymbol.ReturnType, semanticModel);
        }

        private static bool IsSameType(Type type, ITypeSymbol typeSymbol, SemanticModel semanticModel)
        {
            var targetType = semanticModel.Compilation.GetTypeByMetadataName(type.FullName);

            return SymbolEqualityComparer.Default.Equals(typeSymbol, targetType);
        }
    }
}
