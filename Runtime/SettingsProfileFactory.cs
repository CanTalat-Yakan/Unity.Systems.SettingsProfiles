using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// A factory class for creating instances of settings profiles and settings profile managers.
    /// This class provides utility methods for generating settings containers
    /// that manage user-defined or programmatic configurations.
    /// </summary>
    public static class SettingsProfileFactory
    {
        private static readonly object _lock = new();
        private static readonly Dictionary<SettingsProfileCacheUtility.CacheKey, object> _cache = new();

        public static SettingsProfile Create(string profileName = "Default") =>
            GetOrCreate(profileName, name => new SettingsProfile(name));

        public static SettingsProfile<T> Create<T>(string profileName = "Default", Func<T> defaultsFactory = null)
            where T : new() =>
            GetOrCreate(profileName, name => new SettingsProfile<T>(name, defaultsFactory));

        public static SettingsProfileManager CreateManager(string profileName = "Default") =>
            GetOrCreate(profileName, name => new SettingsProfileManager(name));

        public static SettingsProfileManager<T> CreateManager<T>(string profileName = "Default",
            Func<T> defaultsFactory = null) where T : new() =>
            GetOrCreate(profileName, name => new SettingsProfileManager<T>(name, defaultsFactory));

        private static TProfile GetOrCreate<TProfile>(string profileName, Func<string, TProfile> create)
            where TProfile : class
        {
            var name = SettingsProfileCacheUtility.SanitizeName(profileName);
            var key = new SettingsProfileCacheUtility.CacheKey(typeof(TProfile), name);
            var obj = SettingsProfileCacheUtility.GetOrCreateLocked(_lock, _cache, key, () => (object)create(name));
            return (TProfile)obj;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearCacheOnLoad()
        {
            lock (_lock)
                _cache.Clear();
        }
    }
}