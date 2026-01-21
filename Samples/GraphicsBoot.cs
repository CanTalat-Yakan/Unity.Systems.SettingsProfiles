using System;
using UnityEngine;

namespace UnityEssentials.Samples
{
    public class GraphicsBoot : MonoBehaviour
    {
        public static readonly SettingsProfile<GraphicsSettings> Graphics = new("Graphics");
        public static readonly SettingsProfileManager<GraphicsSettings> GraphicsManager = new("GraphicsManager");
        public static readonly SettingsProfile GraphicsDict = new("GraphicsDict");
        public static readonly SettingsProfileManager GraphicsDictManager = new("GraphicsDictManager");

        private void Awake()
        {
            UseGlobalSettingsService();
            UseTypedProfile();
            UseTypedProfileManager();
            UseValueProfile();
            UseValueProfileManager();
        }

        private static void UseGlobalSettingsService()
        {
            var windowModeGlobal = SettingsService.GetInt("window_mode", 3);
            SettingsService.SetInt("window_mode", windowModeGlobal);
        }

        private static void UseTypedProfile()
        {
            Apply(Graphics.Value);
            
            Graphics.OnChanged += Apply;
        }

        private static void UseTypedProfileManager()
        {
            GraphicsManager.SetCurrentProfile("Player2");
            var profile = GraphicsManager.GetCurrentProfile();
            
            var msaa = profile.Value.msaa;
            profile.GetValue().msaa = msaa; 
            
            profile.OnChanged += Apply;
        }

        private static void UseValueProfile()
        {
            var profile = GraphicsDict;

            var windowMode = profile.Value.Get("window_mode", 3);
            var vSync = profile.Value.Get("v-sync", false);
            var masterVolume = profile.Value.Get("master_volume", 100f);

            profile.Value.SetInt("window_mode", windowMode);
            profile.Value.SetBool("v-sync", vSync);
            profile.Value.SetFloat("master_volume", masterVolume);

            var key = "window_mode";
            profile.Value.OnChanged += (key) => ApplyWindowMode(profile.Value.Get(key, 0));
        }

        private static void UseValueProfileManager()
        {
            GraphicsDictManager.SetCurrentProfile("Player2");
            var profile = GraphicsDictManager.GetCurrentProfile();
            
            var volume = profile.Value.GetFloat("master_volume", 80f);
            profile.Value.SetFloat("master_volume", volume);
            
            var key = "window_mode";
            profile.Value.OnChanged += (key) => ApplyWindowMode(profile.Value.Get(key, 0));
        }

        private void OnApplicationQuit()
        {
            SettingsService.Save();
            Graphics.SaveIfDirty();
            GraphicsManager?.GetCurrentProfile().SaveIfDirty();
            GraphicsDict.SaveIfDirty();
            GraphicsDictManager?.GetCurrentProfile().SaveIfDirty();
        }

        private static void Apply(GraphicsSettings g)
        {
            QualitySettings.antiAliasing = g.msaa;
        }
        
        private static void ApplyWindowMode(int mode)
        {
            // Apply window mode logic here
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
