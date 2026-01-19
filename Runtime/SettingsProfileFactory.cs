using System;
using Newtonsoft.Json.Linq;

namespace UnityEssentials
{
    [Serializable]
    public class SettingsProfileBase : SerializedDictionary<string, JToken> { }
    
    /// <summary>
    /// Central creation point for <see cref="SettingsProfile{T}"/>.
    /// </summary>
    public static class SettingsProfileFactory
    {
        public static SettingsProfile Create(string profileName = "Default") =>
            new(profileName);

        public static SettingsProfile<T> Create<T>(string profileName, Func<T> defaultsFactory = null) where T : new() =>
            new(profileName, defaultsFactory);

        public static SettingsProfileManager CreateManager(string profileName = "Default") =>
            new(profileName);

        public static SettingsProfileManager<T> CreateManager<T>(string profileName = "Default", Func<T> defaultsFactory = null) where T : new() =>
            new(profileName, defaultsFactory);
    }
}
