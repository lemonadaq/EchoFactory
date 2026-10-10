using EchoFactory.Core;
using UnityEngine;
namespace EchoFactory.Runtime
{
    // Settings live in PlayerPrefs and are applied to the engine on load and after each change.
    public static class SettingsStore
    {
        private const string VolumeKey="echo-factory-volume";
        private const string FullscreenKey="echo-factory-fullscreen";

        public static GameSettings Load()
        {
            return new GameSettings
            {
                VolumePercent=GameSettings.ClampVolume(PlayerPrefs.GetInt(VolumeKey,GameSettings.DefaultVolumePercent)),
                Fullscreen=PlayerPrefs.GetInt(FullscreenKey,UnityEngine.Screen.fullScreen?1:0)!=0
            };
        }

        public static void Save(GameSettings settings)
        {
            PlayerPrefs.SetInt(VolumeKey,settings.VolumePercent);
            PlayerPrefs.SetInt(FullscreenKey,settings.Fullscreen?1:0);
            PlayerPrefs.Save();
        }

        public static void Apply(GameSettings settings)
        {
            AudioListener.volume=settings.VolumeGain;
            if(UnityEngine.Screen.fullScreen!=settings.Fullscreen)UnityEngine.Screen.fullScreen=settings.Fullscreen;
        }
    }
}
