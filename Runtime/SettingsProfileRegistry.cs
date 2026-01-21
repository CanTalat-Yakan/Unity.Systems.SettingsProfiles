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
        private static readonly Dictionary<SerializerCacheUtility.CacheKey, object> Cache = new();
        private static readonly object Lock = new();

        public static TProfile GetOrCreate<TProfile>(string name, Func<string, TProfile> create) where TProfile : class
        {
            var sanitizedName = SerializerCacheUtility.SanitizeName(name);
            var cacheKey = new SerializerCacheUtility.CacheKey(typeof(TProfile), sanitizedName);
            var cacheObject = SerializerCacheUtility.GetOrCreateLocked(Lock, Cache, cacheKey, () => create(sanitizedName));
            return (TProfile)cacheObject;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearCacheOnLoad()
        {
            lock (Lock) Cache.Clear();
        }
    }
}