using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;

namespace UnityEssentials
{
    public class JsonColorSerializer : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            string colorArray = $"[{value.r}, {value.g}, {value.b}, {value.a}]";
            writer.WriteRawValue(colorArray);
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            float[] values = { 0, 0, 0, 1 };
            if (reader.TokenType == JsonToken.StartArray)
            {
                int i = 0;
                while (reader.Read() && reader.TokenType != JsonToken.EndArray && i < 4)
                    if (reader.Value != null && float.TryParse(reader.Value.ToString(), out float value))
                        values[i++] = value;
            }
            return new Color(values[0], values[1], values[2], values[3]);
        }

        public static T DeserializeColor<T>(object value, T defaultValue)
        {
            if (value is string colorString)
            {
                try
                {
                    var color = JsonConvert.DeserializeObject<Color>(colorString, new JsonColorSerializer());
                    return (T)(object)color;
                }
                catch { return defaultValue; }
            }
            if (value is JArray colorArray && colorArray.Count == 4)
            {
                try
                {
                    float r = colorArray[0].ToObject<float>();
                    float g = colorArray[1].ToObject<float>();
                    float b = colorArray[2].ToObject<float>();
                    float a = colorArray[3].ToObject<float>();
                    return (T)(object)new Color(r, g, b, a);
                }
                catch { return defaultValue; }
            }
            return defaultValue;
        }
    }
}