using System;

namespace UnityEssentials
{
    [Serializable]
    public sealed class SettingsEnvelope<T>
    {
        public string Type;
        public string Profile;
        public int SchemaVersion;
        public string UpdatedUtc;
        public T Values;

        public static SettingsEnvelope<T> Create(string profile, int schemaVersion, T Values)
        {
            return new SettingsEnvelope<T>
            {
                Type = typeof(T).Name,
                Profile = profile,
                SchemaVersion = schemaVersion,
                UpdatedUtc = DateTime.UtcNow.ToString("O"),
                Values = Values
            };
        }
    }
}