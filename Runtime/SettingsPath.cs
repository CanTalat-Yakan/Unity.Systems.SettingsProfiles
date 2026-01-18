using System.IO;
using UnityEngine;

namespace UnityEssentials
{
    public static class SettingsPath
    {
        public static string GetPath<T>(string profile)
        {
            var baseDir = Path.Combine(Application.dataPath, "..", "Resources");
            return Path.Combine(baseDir, $"{Sanitize(profile)}.json");
        }

        private static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "Default" : name.Trim();
        }
    }
}