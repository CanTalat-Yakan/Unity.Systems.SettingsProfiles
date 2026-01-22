using System;
using Newtonsoft.Json.Linq;

namespace UnityEssentials
{
    [Serializable]
    public class SettingsProfileBase : SerializedDictionary<string, JToken> { }

    public sealed class SettingsProfile : SettingsProfile<SettingsProfileBase>
    {
        public new static SettingsProfile GetOrCreate(string name = "Default") =>
            SettingsProfileRegistry.GetOrCreate(name, n => new SettingsProfile(n));

        public SettingsProfile(string name) : base(name) { }

        public new SettingsProfileBase GetValue(bool markDirty = true, bool notify = true) =>
            base.GetValue(markDirty, notify);

        public new SettingsProfileBase Load() =>
            base.Load();
    }

    public class SettingsProfile<T> where T : new()
    {
        public string fileName { get; private set; }
        
        public static SettingsProfile<T> GetOrCreate(string name = "Default") =>
            SettingsProfileRegistry.GetOrCreate(name, n => new SettingsProfile<T>(n));

        /// <summary>
        /// Read access. Ensures the profile is loaded.
        /// For reference types, do not mutate the returned object unless the type raises its own change events
        /// (e.g. SerializedDictionary). For typed settings objects, use <see cref="GetValue"/> for write-intent.
        /// </summary>
        public T Value
        {
            get
            {
                if (!_loaded) Load();
                return _value;
            }
        }

        public bool IsLoaded => _loaded;
        public bool IsDirty => _dirty;

        public event Action<T> OnChanged;

        private Func<T> _defaultsFactory;
        private string _path;

        private bool _loaded;
        private bool _dirty;
        private T _value;

        public SettingsProfile(string name, string extension = "json") =>
            Initialize(SerializerCacheUtility.SanitizeName(name), extension);

        private void Initialize(string name, string extension = null)
        {
            fileName = name;
            _defaultsFactory = () => new T();
            _path = SerializerUtility.GetPath<T>(fileName, extension);
        }

        /// <summary>
        /// Write-intent access.
        /// This is the preferred API for typed settings objects (e.g. GraphicsSettings) where changes cannot be detected.
        /// </summary>
        public T GetValue(bool markDirty = true, bool notify = true)
        {
            if (!_loaded) Load();
            if (markDirty) _dirty = true;
            if (notify) OnChanged?.Invoke(_value);
            return _value;
        }

        public T Load()
        {
            UnhookChangeNotifications(_value);

            _value = _defaultsFactory();
            ApplyValidationAndMigration(_value, null);

            bool dirty;

            if (!SerializerJsonStore.Exists(_path))
            {
                dirty = true; // defaults need a first save if desired
            }
            else
            {
                dirty = false;
                try
                {
                    var json = SerializerJsonStore.ReadAllText(_path);
                    var env = SerializerJson.Deserialize<SerializerEnvelope<T>>(json);

                    if (env != null && env.Values != null)
                    {
                        _value = env.Values;
                        ApplyValidationAndMigration(_value, env.SchemaVersion);
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

            HookChangeNotifications(_value);

            return _value;
        }

        public void SaveIfDirty()
        {
            if (_dirty) Save();
        }

        public void Save()
        {
            ApplyValidationAndMigration(Value, null);

            var schema = (Value is ISettingsVersioned sv) ? sv.SchemaVersion : 0;
            var env = SerializerEnvelope<T>.Create(fileName, schema, Value);

            var json = SerializerJson.Serialize(env);
            SerializerJsonStore.WriteAllTextAtomic(_path, json);
            _dirty = false;
        }

        public void ResetToDefaults(bool save = false, bool notify = true)
        {
            UnhookChangeNotifications(_value);

            _value = _defaultsFactory();
            ApplyValidationAndMigration(_value, null);
            _loaded = true;
            _dirty = true;

            HookChangeNotifications(_value);

            if (notify)
                OnChanged?.Invoke(_value);

            if (save) Save();
        }

        public void DeleteFile()
        {
            SerializerJsonStore.Delete(_path);
            UnhookChangeNotifications(_value);
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

        private void HookChangeNotifications(T v)
        {
            if (v is SerializedDictionary<string, JToken> dict)
                dict.OnChanged += HandleDictionaryChanged;
            else if (v is SerializedDictionary<string, object> objDict)
                objDict.OnChanged += HandleDictionaryChanged;
        }

        private void UnhookChangeNotifications(T v)
        {
            if (v is SerializedDictionary<string, JToken> dict)
                dict.OnChanged -= HandleDictionaryChanged;
            else if (v is SerializedDictionary<string, object> objDict)
                objDict.OnChanged -= HandleDictionaryChanged;
        }

        private void HandleDictionaryChanged(string _)
        {
            _dirty = true;
            OnChanged?.Invoke(_value);
        }
    }
}