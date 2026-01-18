using System;
using Newtonsoft.Json.Linq;

namespace UnityEssentials
{
    [Serializable]
    public class KeyValuePair : SerializedDictionary<string, JToken> { }
    
    /// <summary>
    /// Central creation point for <see cref="SettingsProfile{T}"/>.
    /// </summary>
    public static class SettingsProfileFactory
    {
        public static SettingsProfile<KeyValuePair> Create(
            string profileName = "Default",
            Func<KeyValuePair> defaultsFactory = null) =>
            new(profileName, defaultsFactory ?? (() => new KeyValuePair()));

        public static SettingsProfile<T> Create<T>(string profileName, Func<T> defaultsFactory = null) where T : new() =>
            new(profileName, defaultsFactory: defaultsFactory);

        public static SettingsProfileManager<KeyValuePair> CreateManager(
            string profileName = "Default",
            Func<KeyValuePair> defaultsFactory = null) =>
            new(profileName, defaultsFactory);

        public static SettingsProfileManager<T> CreateManager<T>(string profileName = "Default", Func<T> defaultsFactory = null) where T : new() =>
            new(profileName, defaultsFactory);
    }
}
