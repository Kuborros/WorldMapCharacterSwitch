using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

using FP2Lib.Player;

namespace WorldMapCharacterSwitch
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, "WorldMapCharacterSwap", "1.0.0")]
    //[BepInDependency("000.kuborro.libraries.fp2.fp2lib")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(PatchWorldMap));
        }
    }

    
    internal class PatchWorldMap
    {
        private static bool charChanged;
        private static float fadeTimer = 0f;
        internal static ManualLogSource Logger;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuWorldMap), "Start", MethodType.Normal)]
        static void PatchAdventureStart()
        {
            //TODO: Pull character list from FP2Lib to include customs.
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuClassic), "Start", MethodType.Normal)]
        static void PatchClassicStart()
        {
            //
            //TODO: Pull character list from FP2Lib to include customs.
        }


        //Adventure Map
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuWorldMap),"Update",MethodType.Normal)]
        static void PatchAdventureUpdate(ref float ___movementTimer, int ___currentMap, int ___currentLocation)
        {
            FPCharacterID character = FPSaveManager.character;
            if (character > FPCharacterID.NEERA)
            {
                Logger.LogWarning("Playing as custom character! We can't switch as easily!");
                return;
            }

            if (FPStage.menuInput.specialHold && !charChanged)
            {
                if ((int)character <= 4)
                {
                    character++;
                    if (character == FPCharacterID.BIKECAROL)
                    {
                        //Skip Bike Carol.
                        character++;
                    }
                }
                else
                {
                    //Reset to start
                    character = FPCharacterID.LILAC;
                }
                charChanged = true;
                //Set the new character
                FPSaveManager.character = character;
                //Play transition
                FPScreenTransition component = GameObject.Find("Screen Transition").GetComponent<FPScreenTransition>();
                component.transitionType = FPTransitionTypes.LOCAL_WIPE;
                component.transitionSpeed = 48f;
                component.SetTransitionColor(0f, 0f, 0f);
                component.BeginTransition();
                FPAudio.PlayMenuSfx(3);
            }

            if (charChanged) 
            {
                if (fadeTimer < 24f)
                {
                    fadeTimer += FPStage.deltaTime;
                }
                else
                {
                    ___movementTimer = 0f;
                    FPSaveManager.lastMap = ___currentMap;
                    FPSaveManager.lastMapLocation = ___currentLocation;
                    charChanged = false;
                    FPStage.LoadScene("AdventureMenu");
                }
            }
            else 
                fadeTimer = 0f;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuClassic), "Update", MethodType.Normal)]
        static void PatchClassicUpdate(ref float ___movementTimer, int ___currentTile)
        {
            FPCharacterID character = FPSaveManager.character;
            if (character > FPCharacterID.NEERA)
            {
                Logger.LogWarning("Playing as custom character! We can't switch as easily!");
                return;
            }
            if (FPStage.menuInput.specialHold && !charChanged)
            {
                if ((int)character <= 4 || PlayerHandler.GetPlayableCharaByRuntimeId((int)character) != null)
                {
                    character++;
                    if (character == FPCharacterID.BIKECAROL)
                    {
                        //Skip Bike Carol.
                        character++;
                    }
                }
                else
                {
                    //Reset to start
                    character = FPCharacterID.LILAC;
                }
                charChanged = true;
                //Set the new character
                FPSaveManager.character = character;
                //Play transition
                FPScreenTransition component = GameObject.Find("Screen Transition").GetComponent<FPScreenTransition>();
                component.transitionType = FPTransitionTypes.LOCAL_WIPE;
                component.transitionSpeed = 48f;
                component.SetTransitionColor(0f, 0f, 0f);
                component.BeginTransition();
                FPAudio.PlayMenuSfx(3);
            }

            if (charChanged)
            {
                if (fadeTimer < 24f)
                {
                    fadeTimer += FPStage.deltaTime;
                }
                else
                {
                    ___movementTimer = 0f;
                    FPSaveManager.lastMapLocation = ___currentTile;
                    charChanged = false;
                    FPStage.LoadScene("ClassicMenu");
                }
            }
            else
                fadeTimer = 0f;
        }
    }

}
