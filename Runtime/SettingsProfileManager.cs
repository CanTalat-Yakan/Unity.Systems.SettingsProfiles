using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace UnityEssentials
{
    /// <summary>
    /// Tiny helper to manage multiple named <see cref="SettingsProfile{T}"/> instances and a current selection.
    /// UI, gameplay systems, tools, etc. can all reuse this without taking a dependency on any menu/UI code.
    /// </summary>
    public sealed class SettingsProfileManager : SettingsProfileManager<SerializedDictionary<string, JToken>>
    {
        public SettingsProfileManager(string profileName) : base(profileName, () => new SerializedDictionary<string, JToken>())
        {
        }
        
        public SettingsProfile GetProfile(string profileName) =>
            base.GetProfile(profileName) as SettingsProfile;
        
        public SettingsProfile GetCurrentProfile() =>
            base.GetCurrentProfile() as SettingsProfile;
    }

    public class SettingsProfileManager<T> where T : new()
    {
        public string CurrentProfileName { get; private set; }

        private readonly Func<T> _defaultsFactory;
        private readonly Dictionary<string, SettingsProfile<T>> _profiles = new();

        public SettingsProfileManager(string profileName, Func<T> defaultsFactory = null)
        {
            profileName = string.IsNullOrWhiteSpace(profileName) ? "Default" : profileName.Trim();
            CurrentProfileName = Sanitize(profileName);
            _defaultsFactory = defaultsFactory ?? (() => new T());
        }

        public SettingsProfile<T> GetProfile(string profileName)
        {
            var name = Sanitize(profileName);

            if (_profiles.TryGetValue(name, out var existing))
                return existing;

            var created = new SettingsProfile<T>(name, _defaultsFactory);
            _profiles[name] = created;
            return created;
        }

        public SettingsProfile<T> GetCurrentProfile() =>
            GetProfile(CurrentProfileName);

        public void SetCurrentProfile(string profileName, bool loadIfNeeded = true)
        {
            CurrentProfileName = Sanitize(profileName);
            if (loadIfNeeded)
                GetCurrentProfile().GetOrLoad();
        }

        private static string Sanitize(string name) =>
            string.IsNullOrWhiteSpace(name) ? "Default" : name.Trim();
    }
}