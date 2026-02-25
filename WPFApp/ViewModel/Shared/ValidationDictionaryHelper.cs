using System;
using System.Collections.Generic;

namespace WPFApp.ViewModel.Shared
{
    internal static class ValidationDictionaryHelper
    {
        internal static IReadOnlyDictionary<string, string> RemapFirstErrors(
            IReadOnlyDictionary<string, string> raw,
            Func<string, string> mapKey)
        {
            var mapped = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var kv in raw)
            {
                var key = mapKey(kv.Key);
                if (string.IsNullOrWhiteSpace(key) || mapped.ContainsKey(key))
                    continue;

                mapped[key] = kv.Value;
            }

            return mapped;
        }

        internal static string NormalizeLastSegment(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            key = key.Trim();
            var dot = key.LastIndexOf('.');

            return dot >= 0 && dot < key.Length - 1
                ? key[(dot + 1)..]
                : key;
        }
    }
}
