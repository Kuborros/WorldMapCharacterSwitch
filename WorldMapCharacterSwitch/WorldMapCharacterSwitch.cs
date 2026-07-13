using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using UnityEngine;
using WorldMapCharacterSwitch.Patches;

namespace WorldMapCharacterSwitch
{
    [BepInPlugin("com.kuborro.plugin.fp2.worldmapcharaswap", "WorldMapCharacterSwap", "3.0.0")]
    [BepInDependency("000.kuborro.libraries.fp2.fp2lib")]
    public class WorldMapCharacterSwitch : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        internal static ConfigEntry<bool> allowAllInAdventureMode;
        internal static ConfigEntry<bool> ignoreDisabledInAdventure;

        internal static AssetBundle menuAssets;

        private void Awake()
        {
            allowAllInAdventureMode = Config.Bind("General", "Allow incompatible characters in Adventure", false, "Setting this option will include even characters which might not work due to lacking code or assets");
            ignoreDisabledInAdventure = Config.Bind("General", "Ignore Disabled In Adventure flag", false, "Setting this option will include even characters whose authors disabled them in Adventure Mode");

            string assetPath = Path.Combine(Paths.GameRootPath, "mod_overrides\\CharacterSwitch");
            menuAssets = AssetBundle.LoadFromFile(Path.Combine(assetPath, "characterswitchmenu.assets"));

            if (menuAssets == null)
            {
                Logger.LogError("Failed to load AssetBundles! This mod cannot work without it, exiting. Please reinstall it.");
                //return;
            }
            Harmony.CreateAndPatchAll(typeof(PatchMenus));
        }
    }
}
