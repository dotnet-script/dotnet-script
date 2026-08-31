using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dotnet.Script.Core.Interactive.LineEditing
{
    /// <summary>
    /// Tab completion for the REPL: interactive directives, file paths inside
    /// <c>#load</c>/<c>#r</c>, script variables, keywords and members of known types.
    /// </summary>
    public sealed class ReplCompletionProvider : ICompletionProvider
    {
        private static readonly string[] Directives =
        {
            "#load", "#r", "#exit", "#cls", "#reset"
        };

        private static readonly string[] Keywords =
        {
            "abstract", "as", "async", "await", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "dynamic", "else", "enum",
            "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
            "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "nameof", "namespace", "new",
            "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly",
            "record", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
            "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe",
            "ushort", "using", "var", "virtual", "void", "volatile", "while", "yield"
        };

        private static readonly string[] DefaultNamespaces =
        {
            "System", "System.IO", "System.Collections.Generic", "System.Linq", "System.Text", "System.Threading.Tasks"
        };

        private readonly Func<IEnumerable<string>> _variableNames;
        private readonly Func<string, Type> _variableTypeResolver;
        private readonly Func<string> _currentDirectory;

        private Dictionary<string, Type> _typeCache;
        private int _typeCacheAssemblyCount;

        public ReplCompletionProvider(
            Func<IEnumerable<string>> variableNames = null,
            Func<string, Type> variableTypeResolver = null,
            Func<string> currentDirectory = null)
        {
            _variableNames = variableNames;
            _variableTypeResolver = variableTypeResolver;
            _currentDirectory = currentDirectory ?? Directory.GetCurrentDirectory;
        }

        public CompletionResult GetCompletions(string text, int caret)
        {
            if (text == null)
            {
                return CompletionResult.Empty;
            }

            caret = Math.Max(0, Math.Min(caret, text.Length));

            var lineStart = caret > 0 ? text.LastIndexOf('\n', caret - 1) + 1 : 0;
            var line = text.Substring(lineStart, caret - lineStart);
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                return CompleteDirective(lineStart, line, trimmed, caret);
            }

            return CompleteIdentifier(text, caret);
        }

        /// <summary>
        /// Directives are REPL specific - a language service knows nothing about <c>#exit</c> and friends.
        /// </summary>
        public static bool IsDirectiveLine(string text, int caret)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            caret = Math.Max(0, Math.Min(caret, text.Length));
            var lineStart = caret > 0 ? text.LastIndexOf('\n', caret - 1) + 1 : 0;

            return text.Substring(lineStart, caret - lineStart).TrimStart().StartsWith("#", StringComparison.Ordinal);
        }

        private CompletionResult CompleteDirective(int lineStart, string line, string trimmed, int caret)
        {
            // An odd number of quotes means the caret sits inside an unclosed argument.
            var quoteCount = line.Count(c => c == '"');
            if (quoteCount % 2 == 1)
            {
                var quote = line.LastIndexOf('"');
                var partial = line.Substring(quote + 1);
                if (partial.StartsWith("nuget:", StringComparison.OrdinalIgnoreCase))
                {
                    return CompletionResult.Empty;
                }

                var extension = trimmed.StartsWith("#load", StringComparison.OrdinalIgnoreCase) ? ".csx" : ".dll";
                return CompletePath(lineStart + quote + 1, partial, extension);
            }

            if (quoteCount > 0)
            {
                return CompletionResult.Empty;
            }

            var hashIndex = lineStart + (line.Length - trimmed.Length);
            var items = Filter(Directives, trimmed);
            return new CompletionResult(hashIndex, caret - hashIndex, items);
        }

        private CompletionResult CompletePath(int start, string partial, string extension)
        {
            try
            {
                var separator = partial.LastIndexOfAny(new[] { '/', '\\' });
                var directoryPart = separator >= 0 ? partial.Substring(0, separator + 1) : string.Empty;
                var filePart = separator >= 0 ? partial.Substring(separator + 1) : partial;

                var searchRoot = directoryPart.Length == 0
                    ? _currentDirectory()
                    : Path.IsPathRooted(directoryPart) ? directoryPart : Path.Combine(_currentDirectory(), directoryPart);

                if (!Directory.Exists(searchRoot))
                {
                    return CompletionResult.Empty;
                }

                var items = Directory.GetDirectories(searchRoot)
                    .Select(d => Path.GetFileName(d) + "/")
                    .Concat(Directory.GetFiles(searchRoot)
                        .Where(f => string.Equals(Path.GetExtension(f), extension, StringComparison.OrdinalIgnoreCase))
                        .Select(Path.GetFileName))
                    .Where(name => name.StartsWith(filePart, StringComparison.OrdinalIgnoreCase))
                    .Select(name => directoryPart + name)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return new CompletionResult(start, partial.Length, items);
            }
            catch (Exception)
            {
                // Unreadable or invalid path - simply offer nothing.
                return CompletionResult.Empty;
            }
        }

        private CompletionResult CompleteIdentifier(string text, int caret)
        {
            var start = caret;
            while (start > 0 && LineBuffer.IsWordChar(text[start - 1]))
            {
                start--;
            }

            var prefix = text.Substring(start, caret - start);

            if (start > 0 && text[start - 1] == '.')
            {
                var chain = ExtractChain(text, start - 1);
                if (chain == null)
                {
                    return CompletionResult.Empty;
                }

                var type = ResolveChainType(chain);
                if (type == null)
                {
                    return CompletionResult.Empty;
                }

                return new CompletionResult(start, prefix.Length, Filter(GetMemberNames(type), prefix));
            }

            var candidates = new List<string>();
            if (_variableNames != null)
            {
                try
                {
                    candidates.AddRange(_variableNames() ?? Enumerable.Empty<string>());
                }
                catch (Exception)
                {
                    // The script state may be in a faulted state; ignore.
                }
            }
            candidates.AddRange(Keywords);

            return new CompletionResult(start, prefix.Length, Filter(candidates, prefix));
        }

        private static IReadOnlyList<string> Filter(IEnumerable<string> candidates, string prefix) =>
            candidates
                .Where(c => !string.IsNullOrEmpty(c) && c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        /// <summary>
        /// Walks backwards from a dot collecting the <c>a.b.c</c> chain preceding it.
        /// Returns null when the expression is not a plain member access chain.
        /// </summary>
        private static List<string> ExtractChain(string text, int dotIndex)
        {
            var chain = new List<string>();
            var index = dotIndex;

            while (true)
            {
                var end = index;
                var segmentStart = end;
                while (segmentStart > 0 && LineBuffer.IsWordChar(text[segmentStart - 1]))
                {
                    segmentStart--;
                }

                if (segmentStart == end)
                {
                    return null;
                }

                chain.Insert(0, text.Substring(segmentStart, end - segmentStart));

                if (segmentStart > 0 && text[segmentStart - 1] == '.')
                {
                    index = segmentStart - 1;
                    continue;
                }

                return chain;
            }
        }

        private Type ResolveChainType(List<string> chain)
        {
            Type current = null;

            if (_variableTypeResolver != null)
            {
                try
                {
                    current = _variableTypeResolver(chain[0]);
                }
                catch (Exception)
                {
                    current = null;
                }
            }

            if (current == null)
            {
                current = ResolveTypeName(chain[0]);
            }

            for (var i = 1; i < chain.Count && current != null; i++)
            {
                current = GetMemberType(current, chain[i]);
            }

            return current;
        }

        private static Type GetMemberType(Type type, string memberName)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

            var property = type.GetProperty(memberName, flags);
            if (property != null)
            {
                return property.PropertyType;
            }

            var field = type.GetField(memberName, flags);
            return field?.FieldType;
        }

        private static IEnumerable<string> GetMemberNames(Type type)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

            try
            {
                return type.GetMembers(flags)
                    .Where(m => m.MemberType != MemberTypes.Constructor)
                    .Select(m => m.Name)
                    .Where(name => name.IndexOf('_') != 0 && !IsAccessorName(name))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
            }
            catch (Exception)
            {
                return Enumerable.Empty<string>();
            }
        }

        private static bool IsAccessorName(string name) =>
            name.StartsWith("get_", StringComparison.Ordinal) ||
            name.StartsWith("set_", StringComparison.Ordinal) ||
            name.StartsWith("add_", StringComparison.Ordinal) ||
            name.StartsWith("remove_", StringComparison.Ordinal) ||
            name.StartsWith("op_", StringComparison.Ordinal);

        private Type ResolveTypeName(string name)
        {
            var cache = GetTypeCache();
            return cache.TryGetValue(name, out var type) ? type : null;
        }

        private Dictionary<string, Type> GetTypeCache()
        {
            Assembly[] assemblies;
            try
            {
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }
            catch (Exception)
            {
                return _typeCache ?? new Dictionary<string, Type>(StringComparer.Ordinal);
            }

            if (_typeCache != null && assemblies.Length == _typeCacheAssemblyCount)
            {
                return _typeCache;
            }

            var cache = new Dictionary<string, Type>(StringComparer.Ordinal);
            var namespaces = new HashSet<string>(DefaultNamespaces, StringComparer.Ordinal);

            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetExportedTypes();
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (var type in types)
                {
                    if (type.Namespace == null || !namespaces.Contains(type.Namespace) || type.IsNested)
                    {
                        continue;
                    }

                    if (!cache.ContainsKey(type.Name))
                    {
                        cache[type.Name] = type;
                    }
                }
            }

            _typeCache = cache;
            _typeCacheAssemblyCount = assemblies.Length;
            return cache;
        }
    }
}
