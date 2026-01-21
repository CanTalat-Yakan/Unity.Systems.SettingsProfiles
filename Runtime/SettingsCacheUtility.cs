using System;
using System.Collections.Generic;

namespace UnityEssentials
{
    /// <summary>
    /// Utility class for handling caching mechanisms related to settings profiles.
    /// Provides methods for creating, sanitizing, and managing cache entries to minimize redundant computations
    /// when working with profile-related operations.
    /// </summary>
    internal static class SettingsCacheUtility
    {
        internal readonly struct CacheKey : IEquatable<CacheKey>
        {
            public readonly Type Type;
            public readonly string Name;

            public CacheKey(Type type, string name)
            {
                Type = type;
                Name = name;
            }

            public bool Equals(CacheKey other) =>
                Type == other.Type && string.Equals(Name, other.Name, StringComparison.Ordinal);

            public override bool Equals(object obj) => obj is CacheKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Type, Name);
        }

        public static string SanitizeName(string name) =>
            string.IsNullOrWhiteSpace(name) ? "Default" : name.Trim();

        public static TValue GetOrCreate<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key, Func<TValue> create)
        {
            if (dict == null) throw new ArgumentNullException(nameof(dict));
            if (create == null) throw new ArgumentNullException(nameof(create));

            if (dict.TryGetValue(key, out var existing))
                return existing;

            var created = create();
            dict[key] = created;
            return created;
        }

        public static TValue GetOrCreateLocked<TKey, TValue>(object gate, Dictionary<TKey, TValue> dict, TKey key,
            Func<TValue> create, Action onExisting = null)
        {
            if (gate == null) throw new ArgumentNullException(nameof(gate));
            if (dict == null) throw new ArgumentNullException(nameof(dict));
            if (create == null) throw new ArgumentNullException(nameof(create));

            lock (gate)
            {
                if (dict.TryGetValue(key, out var existing))
                {
                    onExisting?.Invoke();
                    return existing;
                }

                var created = create();
                dict[key] = created;
                return created;
            }
        }
    }
}