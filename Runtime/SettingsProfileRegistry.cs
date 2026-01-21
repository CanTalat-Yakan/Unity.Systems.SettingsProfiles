using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Shared cache for SettingsProfile and SettingsProfileManager instances.
    /// This enables using constructors directly while still returning the same cached instance
    /// per type + sanitized profile name.
    /// </summary>
    internal static class SettingsProfileRegistry
    {
        private static readonly Dictionary<SettingsCacheUtility.CacheKey, object> s_cache = new();
        private static readonly object s_lock = new();

        public static TProfile GetOrCreate<TProfile>(string name, Func<string, TProfile> create) where TProfile : class
        {
            var sanitizedName = SettingsCacheUtility.SanitizeName(name);
            var cacheKey = new SettingsCacheUtility.CacheKey(typeof(TProfile), sanitizedName);
            var cacheObject =
                SettingsCacheUtility.GetOrCreateLocked(s_lock, s_cache, cacheKey, () => create(sanitizedName));
            return (TProfile)cacheObject;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearCacheOnLoad()
        {
            lock (s_lock) s_cache.Clear();
        }
    }
}