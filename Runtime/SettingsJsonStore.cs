using System.IO;

namespace UnityEssentials
{
    public static class SettingsJsonStore
    {
        public static bool Exists(string path) => File.Exists(path);

        public static string ReadAllText(string path) => File.ReadAllText(path);

        public static void WriteAllTextAtomic(string path, string content)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var tmp = path + ".tmp";
            File.WriteAllText(tmp, content);

            // Replace atomically where supported; fallback to Move.
            if (File.Exists(path))
            {
                try
                {
                    File.Replace(tmp, path, null);
                    return;
                }
                catch
                {
                    File.Delete(path);
                }
            }

            File.Move(tmp, path);
        }

        public static void Delete(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

}