using System;
using UnityEngine;

namespace UnityEssentials.Samples
{
    public class GraphicsBoot : MonoBehaviour
    {
        public static readonly SettingsProfile<GraphicsSettings> Graphics =
            SettingsProfileFactory.Create("Graphics", () => new GraphicsSettings());

        public static readonly SettingsProfileManager<GraphicsSettings> GraphicsManager =
            SettingsProfileFactory.CreateManager("GraphicsManager", () => new GraphicsSettings());

        public static readonly SettingsProfile<KeyValuePair> GraphicsKvp =
            SettingsProfileFactory.Create("GraphicsKvp");

        public static readonly SettingsProfileManager<KeyValuePair> GraphicsKvpManager =
            SettingsProfileFactory.CreateManager("GraphicsKvpManager");

        private void Awake()
        {
            UseGlobalSettingsService();
            UseTypedProfile();
            UseTypedProfileManager();
            UseKeyValueProfile();
            UseKeyValueProfileManager();
        }

        // Global key/value sample (SettingsService)
        private static void UseGlobalSettingsService()
        {
            var windowModeGlobal = SettingsService.GetInt("window_mode", 3);
            SettingsService.SetInt("window_mode", windowModeGlobal);
        }

        private static void UseTypedProfile()
        {
            Apply(Graphics.GetOrLoad());
            Graphics.Changed += Apply;
            GraphicsManager.GetCurrentProfile().Changed += Apply;
        }

        private static void UseTypedProfileManager()
        {
            // Same idea as the KV manager, but managing typed settings objects.
            GraphicsManager.SetCurrentProfile("Player2", loadIfNeeded: true);

            var current = GraphicsManager.GetCurrentProfile();
            var g = current.GetOrLoad();

            // Make a tiny, visible mutation to show the pattern.
            current.Mutate(s => s.msaa = g.msaa, notify: false);
        }

        private static void UseKeyValueProfile()
        {
            var kv = GraphicsKvp.GetOrLoad();

            var windowMode = kv.GetInt("window_mode", 3);
            var vSync = kv.GetBool("v-sync");
            var masterVolume = kv.GetFloat("master_volume", 100f);

            GraphicsKvp.Mutate(s =>
            {
                s.SetInt("window_mode", windowMode);
                s.SetBool("v-sync", vSync);
                s.SetFloat("master_volume", masterVolume);
            }, notify: false);
        }

        private static void UseKeyValueProfileManager()
        {
            // This is a "service-like" pattern for multiple named profiles ("Default", "Player2", etc.).
            GraphicsKvpManager.SetCurrentProfile("Player2", loadIfNeeded: true);

            var current = GraphicsKvpManager.GetCurrentProfile();
            var p2Volume = current.GetOrLoad().GetFloat("master_volume", 80f);
            current.Mutate(s => s.SetFloat("master_volume", p2Volume), notify: false);
        }

        private void OnApplicationQuit()
        {
            SettingsService.Save();
            Graphics.SaveIfDirty();
            GraphicsManager?.GetCurrentProfile().SaveIfDirty();
            GraphicsKvp.SaveIfDirty();
            GraphicsKvpManager?.GetCurrentProfile().SaveIfDirty();
        }

        private static void Apply(GraphicsSettings g)
        {
            QualitySettings.antiAliasing = g.msaa;
        }
    }

    [Serializable]
    public sealed class GraphicsSettings : ISettingsVersioned, ISettingsValidate, ISettingsMigrate
    {
        public int SchemaVersion => 2;

        [Range(0.5f, 2f)] public float renderScale = 1f;
        [Range(0, 4)] public int msaa = 2;
        public bool hdr = true;

        // Introduced in v2
        public int anisotropicFiltering = 8;

        public void Validate()
        {
            renderScale = Mathf.Clamp(renderScale, 0.5f, 2f);
            msaa = Mathf.Clamp(msaa, 0, 4);
            anisotropicFiltering = Mathf.Clamp(anisotropicFiltering, 1, 16);
        }

        public bool TryMigrate(int fromVersion)
        {
            if (fromVersion < 2)
            {
                // v1 had no anisotropicFiltering; choose a conservative default
                anisotropicFiltering = 8;
                return true;
            }
            return false;
        }
    }
}
