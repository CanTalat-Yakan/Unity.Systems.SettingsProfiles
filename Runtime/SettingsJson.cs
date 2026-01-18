using Newtonsoft.Json;

namespace UnityEssentials
{
    /// <summary>
    /// Central place for Json.NET settings used by SettingsProfile.
    /// Keeps converters/resolvers consistent across the project.
    /// </summary>
    public static class SettingsJson
    {
        /// <summary>
        /// Default serializer settings used for reading/writing settings profiles.
        /// </summary>
        public static JsonSerializerSettings DefaultSerializerSettings => new()
        {
            Formatting = Formatting.Indented,
            ContractResolver = new JsonPropertyFilter(),
            Converters = { new JsonColorSerializer() },
        };

        public static string Serialize<T>(T value) =>
            JsonConvert.SerializeObject(value, DefaultSerializerSettings);

        public static T Deserialize<T>(string json) =>
            JsonConvert.DeserializeObject<T>(json, DefaultSerializerSettings);
    }
}
