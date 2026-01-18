using System;
using System.Collections.Generic;

namespace UnityEssentials
{
    /// <summary>
    /// Tiny helper to manage multiple named <see cref="SettingsProfile{T}"/> instances and a current selection.
    /// UI, gameplay systems, tools, etc. can all reuse this without taking a dependency on any menu/UI code.
    /// </summary>
    public sealed class SettingsProfileManager<T> where T : new()
    {
        public string CurrentProfileName { get; private set; }

        private readonly Func<T> _defaultsFactory;
        private readonly Dictionary<string, SettingsProfile<T>> _profiles = new();

        internal SettingsProfileManager(string profileName = "Default", Func<T> defaultsFactory = null)
        {
            _defaultsFactory = defaultsFactory ?? (() => new T());
            CurrentProfileName = Sanitize(profileName);
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

        public SettingsProfile<T> GetCurrentProfile() => GetProfile(CurrentProfileName);

        public void SetCurrentProfile(string profileName, bool loadIfNeeded = true)
        {
            CurrentProfileName = Sanitize(profileName);
            if (loadIfNeeded)
                GetCurrentProfile().GetOrLoad();
        }

        private static string Sanitize(string name)
        {
            return string.IsNullOrWhiteSpace(name) ? "Default" : name.Trim();
        }
    }
}
