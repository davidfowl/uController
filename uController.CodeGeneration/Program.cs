using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace uController.CodeGeneration
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Required path to dll");
                return;
            }

            var path = args[0];
            var outputPath = args.Length > 1 ? args[1] : null;
            var directory = Path.GetDirectoryName(path);
            var resolver = new MyResolver(directory);
            var metadataLoadContext = new MetadataLoadContext(resolver, typeof(object).Assembly.FullName);
            var uControllerAssembly = metadataLoadContext.LoadFromAssemblyName(typeof(HttpHandler).Assembly.FullName);
            var handler = uControllerAssembly.GetType(typeof(HttpHandler).FullName);
            var assembly = metadataLoadContext.LoadFromAssemblyPath(path);

            var models = new List<HttpModel>();

            foreach (var type in assembly.GetExportedTypes())
            {
                if (handler.IsAssignableFrom(type))
                {
                    var model = HttpModel.FromType(type);
                    models.Add(model);
                }
            }

            if (models.Count > 0 && outputPath != null)
            {
                Directory.CreateDirectory(outputPath);
            }

            foreach (var model in models)
            {
                var gen = new CodeGenerator(model, metadataLoadContext);
                if (outputPath != null)
                {
                    var fileName = Path.Combine(outputPath, model.HandlerType.Name + ".RouteProvider.cs");
                    File.WriteAllText(fileName, gen.Generate());
                }
                else
                {
                    Console.WriteLine(gen.Generate());
                }
            }
        }
    }

    internal class MyResolver : MetadataAssemblyResolver
    {
        private string[] _directory;

        public MyResolver(string directory)
        {
            _directory = new[] {
                directory, // application
                Path.GetDirectoryName(typeof(object).Assembly.Location), // .NET Core
                Path.GetDirectoryName(typeof(IApplicationBuilder).Assembly.Location) // ASP.NET Core
            };
        }

        public override Assembly Resolve(MetadataLoadContext context, AssemblyName assemblyName)
        {
            foreach (var d in _directory)
            {
                var path = Path.Combine(d, assemblyName.Name + ".dll");
                if (File.Exists(path))
                {
                    return context.LoadFromAssemblyPath(path);
                }
            }
            return null;
        }
    }
}
