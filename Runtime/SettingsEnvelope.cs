using System;

namespace UnityEssentials
{
    [Serializable]
    public sealed class SettingsEnvelope<T>
    {
        public string type;
        public string profile;
        public int schemaVersion;
        public string updatedUtc;
        public T data;

        public static SettingsEnvelope<T> Create(string profile, int schemaVersion, T data)
        {
            return new SettingsEnvelope<T>
            {
                type = typeof(T).Name,
                profile = profile,
                schemaVersion = schemaVersion,
                updatedUtc = DateTime.UtcNow.ToString("O"),
                data = data
            };
        }
    }
}