using System;
using System.Collections.Generic;

namespace UnityEssentials
{
    /// <summary>
    /// Manages multiple named settings profiles and allows for the selection and manipulation
    /// of the current profile. Provides utilities to retrieve, modify, and switch between profiles,
    /// with enhancements for profile loading and creation.
    /// </summary>
    public sealed class SettingsProfileManager
    {
        private readonly SettingsProfileManagerBase<SettingsProfile> _base;

        public static SettingsProfileManager GetOrCreate(string name = "Default") =>
            SettingsProfileRegistry.GetOrCreate(name, n => new SettingsProfileManager(n));

        public SettingsProfileManager(string name) =>
            _base = new SettingsProfileManagerBase<SettingsProfile>(name, n => new SettingsProfile(n));

        public SettingsProfile GetProfile(string name) =>
            _base.GetProfile(name);

        public SettingsProfile GetCurrentProfile() =>
            _base.GetCurrentProfile();

        public void SetCurrentProfile(string name, bool load = true) =>
            _base.SetCurrentProfile(name, load, p => p.Load());
    }

    /// <summary>
    /// Tiny helper to manage multiple named <see cref="SettingsProfile{T}"/> instances and a current selection.
    /// </summary>
    public sealed class SettingsProfileManager<T> where T : new()
    {
        private readonly SettingsProfileManagerBase<SettingsProfile<T>> _base;

        public static SettingsProfileManager<T> GetOrCreate(string name = "Default") =>
            SettingsProfileRegistry.GetOrCreate(name, n => new SettingsProfileManager<T>(n));

        public SettingsProfileManager(string name) =>
            _base = new SettingsProfileManagerBase<SettingsProfile<T>>(name, n => new SettingsProfile<T>(n));

        public SettingsProfile<T> GetProfile(string name) =>
            _base.GetProfile(name);

        public SettingsProfile<T> GetCurrentProfile() =>
            _base.GetCurrentProfile();

        public void SetCurrentProfile(string profileName, bool load = true) =>
            _base.SetCurrentProfile(profileName, load, p => p.Load());
    }

    internal sealed class SettingsProfileManagerBase<TProfile>
    {
        public string CurrentProfileName { get; private set; }

        private readonly Dictionary<string, TProfile> _profiles = new();
        private readonly Func<string, TProfile> _createProfile;

        public SettingsProfileManagerBase(string profileName, Func<string, TProfile> createProfile)
        {
            _createProfile = createProfile ?? throw new ArgumentNullException(nameof(createProfile));
            CurrentProfileName = SettingsCacheUtility.SanitizeName(profileName);
        }

        public TProfile GetProfile(string profileName)
        {
            var name = SettingsCacheUtility.SanitizeName(profileName);
            return SettingsCacheUtility.GetOrCreate(_profiles, name, () => _createProfile(name));
        }

        public TProfile GetCurrentProfile() =>
            GetProfile(CurrentProfileName);

        public void SetCurrentProfile(string profileName, bool load, Action<TProfile> loadAction)
        {
            CurrentProfileName = SettingsCacheUtility.SanitizeName(profileName);
            if (load) loadAction?.Invoke(GetCurrentProfile());
        }
    }
}