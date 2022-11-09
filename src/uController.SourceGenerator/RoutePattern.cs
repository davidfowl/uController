using System;
using System.Collections.Generic;
using System.Linq;

namespace uController.SourceGenerator
{
    class RoutePattern
    {
        private static readonly char[] Slash = new[] { '/' };

        public string Pattern { get; }

        private string[] _parameterNames;

        public RoutePattern(string pattern, string[] parameterNames)
        {
            Pattern = pattern;
            _parameterNames = parameterNames;
        }

        public bool HasParameter(string name) => _parameterNames.Contains(name);

        public override string ToString() => Pattern;

        public static RoutePattern Parse(string pattern)
        {
            if (pattern is null)
            {
                return null;
            }

            var segments = pattern.Split(Slash, StringSplitOptions.RemoveEmptyEntries);

            List<string> parameters = null;
            foreach (var s in segments)
            {
                // Ignore complex segments and escaping

                var start = s.IndexOf('{');
                if (start != -1)
                {
                    var end = s.IndexOf('}', start + 1);

                    if (end == -1)
                    {
                        continue;
                    }

                    var p = s.Substring(start + 1, end - start - 1);
                    var constraintToken = p.IndexOf(':');

                    if (constraintToken != -1)
                    {
                        // Remove the constraint
                        p = p.Substring(0, constraintToken);
                    }

                    parameters ??= new();
                    parameters.Add(p);
                }
            }

            return new RoutePattern(pattern, parameters?.ToArray() ?? Array.Empty<string>());
        }
    }
}
