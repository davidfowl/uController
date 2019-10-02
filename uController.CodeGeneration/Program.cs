using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            // input.dll outputDir references.txt

            var path = args[0];
            var outputPath = args.Length > 1 ? args[1] : null;
            var directory = Path.GetDirectoryName(path);
            var referencePaths = args.Length > 2 ? File.ReadAllLines(args[2]) : new string[0];
            var resolver = new PathAssemblyResolver(referencePaths);
            var corAssembly = referencePaths.Where(m => m.Contains("mscorlib")).Select(a => AssemblyName.GetAssemblyName(a).FullName).FirstOrDefault();
            var metadataLoadContext = new MetadataLoadContext(resolver, corAssembly);
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
}
