using System;

namespace UnityEssentials
{
    public sealed class SettingsProfile<T> where T : new()
    {
        public string ProfileName { get; }
        public T Value => _value;
        public bool IsLoaded => _loaded;
        public bool IsDirty => _dirty;

        public event Action<T> Changed;

        private readonly Func<T> _defaultsFactory;
        private readonly string _path;

        private bool _loaded;
        private bool _dirty;
        private T _value;

        public SettingsProfile(string profileName, Func<T> defaultsFactory = null)
        {
            ProfileName = string.IsNullOrWhiteSpace(profileName) ? "Default" : profileName.Trim();
            _defaultsFactory = defaultsFactory ?? (() => new T());
            _path = SettingsPath.GetPath<T>(ProfileName);
        }

        public T GetOrLoad()
        {
            if (_loaded) return _value;
            Load();
            return _value;
        }

        public void Load()
        {
            _value = _defaultsFactory();
            ApplyValidationAndMigration(_value, null);

            bool dirty;

            if (!SettingsJsonStore.Exists(_path))
            {
                dirty = true; // defaults need a first save if desired
            }
            else
            {
                dirty = false;
                try
                {
                    var json = SettingsJsonStore.ReadAllText(_path);
                    var env = SettingsJson.Deserialize<SettingsEnvelope<T>>(json);

                    if (env != null && env.data != null)
                    {
                        _value = env.data;
                        ApplyValidationAndMigration(_value, env.schemaVersion);
                    }
                    else
                    {
                        dirty = true;
                    }
                }
                catch
                {
                    dirty = true;
                }
            }

            _loaded = true;
            _dirty = dirty;
        }

        public void Set(T newValue, bool markDirty = true, bool notify = true)
        {
            _value = newValue;
            _loaded = true;
            if (markDirty) _dirty = true;
            if (notify) Changed?.Invoke(_value);
        }

        public void Mutate(Action<T> edit, bool notify = true)
        {
            var v = GetOrLoad();
            edit?.Invoke(v);
            ApplyValidationAndMigration(v, null);
            _dirty = true;
            if (notify) Changed?.Invoke(_value);
        }

        public void SaveIfDirty()
        {
            if (_dirty) Save();
        }
        
        public void Save()
        {
            var v = GetOrLoad();
            ApplyValidationAndMigration(v, null);

            var schema = (v is ISettingsVersioned sv) ? sv.SchemaVersion : 0;
            var env = SettingsEnvelope<T>.Create(ProfileName, schema, v);

            var json = SettingsJson.Serialize(env);
            SettingsJsonStore.WriteAllTextAtomic(_path, json);
            _dirty = false;
        }

        public void ResetToDefaults(bool save = false)
        {
            _value = _defaultsFactory();
            ApplyValidationAndMigration(_value, null);
            _loaded = true;
            _dirty = true;
            Changed?.Invoke(_value);
            if (save) Save();
        }

        public void DeleteFile()
        {
            SettingsJsonStore.Delete(_path);
            _loaded = false;
            _dirty = false;
        }

        private static void ApplyValidationAndMigration(T v, int? fromVersion)
        {
            if (fromVersion.HasValue && v is ISettingsMigrate mig)
                // If migration fails, proceed with whatever state v has (caller decides).
                mig.TryMigrate(fromVersion.Value);

            if (v is ISettingsValidate val)
                val.Validate();
        }
    }
}