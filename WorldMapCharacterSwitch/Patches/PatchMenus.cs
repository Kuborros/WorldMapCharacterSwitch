using HarmonyLib;
using System;
using UnityEngine;
using WorldMapCharacterSwitch.Objects;

namespace WorldMapCharacterSwitch.Patches
{
    internal class PatchMenus
    {
        private static GameObject switchMenuMaster = null;
        private static FPObjectState waitForMenuDelegate;

        //Adventure Map
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuWorldMap), "Start", MethodType.Normal)]
        static void PatchAdventureStart(MenuWorldMap __instance)
        {
            switchMenuMaster = WorldMapCharacterSwitch.menuAssets.LoadAsset<GameObject>("CharacterSelectMenu");
            waitForMenuDelegate = AccessTools.MethodDelegate<FPObjectState>(AccessTools.Method(typeof(MenuWorldMap),"State_WaitForMenu") ,__instance);
            MenuCharacterSwitch.AssembleCharacterList();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuWorldMap), "State_Default", MethodType.Normal)]
        static void PatchAdventureDefault(MenuWorldMap __instance, ref GameObject ___targetMenu, int ___currentMap, int ___currentLocation, FPMap[] ___maps)
        {
            if (FPStage.menuInput.specialHold)
            {
                GameObject switchMenu = GameObject.Instantiate(switchMenuMaster);
                switchMenu.SetActive(true);
                ___targetMenu = switchMenu;
                __instance.textHeader.SetActive(false);

                if (___maps[___currentMap].locations[___currentLocation].type != FPMapLocationType.NONE)
                {
                    //Only set the map tile if we are in a 'safe' location that allows input. This prevents horrible softlocks.
                    FPSaveManager.lastMapLocation = ___currentLocation;
                    FPSaveManager.lastMap = ___currentMap;
                }
                else
                {
                    //The position in file might belong to previous world map, so if its something stupid we need to reset it.
                    WorldMapCharacterSwitch.Logger.LogDebug("Player on unsafe tile! Resetting position.");
                }

                __instance.state = waitForMenuDelegate;
                FPAudio.PlayMenuSfx(2);
            }
        }

        //Classic Map
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuClassic), "Start", MethodType.Normal)]
        static void PatchClassicStart(MenuClassic __instance)
        {
            switchMenuMaster = WorldMapCharacterSwitch.menuAssets.LoadAsset<GameObject>("CharacterSelectMenu");
            waitForMenuDelegate = AccessTools.MethodDelegate<FPObjectState>(AccessTools.Method(typeof(MenuClassic), "State_WaitForMenu"), __instance);
            MenuCharacterSwitch.AssembleCharacterList();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuClassic), "State_Default", MethodType.Normal)]
        static void PatchClassicUpdate(MenuClassic __instance, ref GameObject ___targetMenu, int ___currentTile)
        {
            if (FPStage.menuInput.specialHold)
            {
                GameObject switchMenu = GameObject.Instantiate(switchMenuMaster);
                switchMenu.SetActive(true);
                ___targetMenu = switchMenu;

                FPSaveManager.lastMapLocation = ___currentTile;

                __instance.state = waitForMenuDelegate;
                FPAudio.PlayMenuSfx(2);
            }
        }

        //Menu Replacements
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MenuBasic), "Start", MethodType.Normal)]
        static void PatchMenuBasicStart(MenuBasic __instance)
        {
            //Swap to out own Menu implementation
            if (__instance.gameObject.name.Contains("CharacterSelectMenu"))
            {
                __instance.gameObject.AddComponent<MenuCharacterSwitch>();
                GameObject.Destroy(__instance);
            }
        }
    }
}
